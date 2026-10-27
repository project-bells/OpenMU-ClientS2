#include "stdafx.h"
#include "AntiCheat/ClientAntiCheat.h"

#include <algorithm>
#include <array>
#include <cctype>
#include <cstring>
#include <cwctype>
#include <fstream>
#include <iomanip>
#include <sstream>

#include <openssl/evp.h>
#include <openssl/hmac.h>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <TlHelp32.h>
#include <Windows.h>
#endif

namespace Origins::AntiCheat
{
namespace
{
constexpr std::string_view BuildId = "origins-s6e3-phase9-1";

void Log(std::string_view message)
{
    std::ofstream log("OriginsAntiCheat.log", std::ios::app);
    log << message << '\n';
}

std::string Hex(const unsigned char* data, std::size_t size)
{
    std::ostringstream output;
    output << std::hex << std::setfill('0');
    for (std::size_t i = 0; i < size; ++i) output << std::setw(2) << static_cast<unsigned int>(data[i]);
    return output.str();
}

std::string Sha256(const void* data, std::size_t size)
{
    std::array<unsigned char, EVP_MAX_MD_SIZE> digest{};
    unsigned int length{};
    EVP_Digest(data, size, digest.data(), &length, EVP_sha256(), nullptr);
    return Hex(digest.data(), length);
}

bool EqualIgnoreCase(std::string left, std::string right)
{
    auto lower = [](unsigned char value) { return static_cast<char>(std::tolower(value)); };
    std::ranges::transform(left, left.begin(), lower);
    std::ranges::transform(right, right.begin(), lower);
    return left == right;
}

std::string HashFile(const std::filesystem::path& path)
{
    std::ifstream input(path, std::ios::binary);
    if (!input) return {};
    EVP_MD_CTX* context = EVP_MD_CTX_new();
    EVP_DigestInit_ex(context, EVP_sha256(), nullptr);
    std::array<char, 64 * 1024> buffer{};
    while (input)
    {
        input.read(buffer.data(), buffer.size());
        if (input.gcount() > 0) EVP_DigestUpdate(context, buffer.data(), static_cast<std::size_t>(input.gcount()));
    }
    std::array<unsigned char, EVP_MAX_MD_SIZE> digest{};
    unsigned int length{};
    EVP_DigestFinal_ex(context, digest.data(), &length);
    EVP_MD_CTX_free(context);
    return Hex(digest.data(), length);
}

#ifdef _WIN32
std::string EnvironmentUtf8(const wchar_t* name)
{
    std::array<wchar_t, 1024> value{};
    const DWORD length = GetEnvironmentVariableW(name, value.data(), static_cast<DWORD>(value.size()));
    if (length == 0 || length >= value.size()) return {};
    const int bytes = WideCharToMultiByte(CP_UTF8, 0, value.data(), static_cast<int>(length), nullptr, 0, nullptr, nullptr);
    std::string result(bytes, '\0');
    WideCharToMultiByte(CP_UTF8, 0, value.data(), static_cast<int>(length), result.data(), bytes, nullptr, nullptr);
    return result;
}

std::filesystem::path ExecutablePath()
{
    std::array<wchar_t, 32768> path{};
    const DWORD length = GetModuleFileNameW(nullptr, path.data(), static_cast<DWORD>(path.size()));
    return std::filesystem::path(std::wstring(path.data(), length));
}

std::string TextSectionHash()
{
    const auto base = reinterpret_cast<const unsigned char*>(GetModuleHandleW(nullptr));
    if (!base) return {};
    const auto dos = reinterpret_cast<const IMAGE_DOS_HEADER*>(base);
    if (dos->e_magic != IMAGE_DOS_SIGNATURE) return {};
    const auto nt = reinterpret_cast<const IMAGE_NT_HEADERS*>(base + dos->e_lfanew);
    if (nt->Signature != IMAGE_NT_SIGNATURE) return {};
    const auto section = IMAGE_FIRST_SECTION(nt);
    for (unsigned int i = 0; i < nt->FileHeader.NumberOfSections; ++i)
    {
        if (std::memcmp(section[i].Name, ".text", 5) == 0)
        {
            return Sha256(base + section[i].VirtualAddress, section[i].Misc.VirtualSize);
        }
    }
    return {};
}
#else
std::filesystem::path ExecutablePath() { return {}; }
std::string TextSectionHash() { return {}; }
#endif
}

void ViolationReporter::Report(std::string_view code, int points)
{
    std::scoped_lock lock(mutex_);
    if (std::ranges::find(codes_, code) != codes_.end()) return;
    riskScore_ = std::min(100, riskScore_ + std::max(0, points));
    codes_.emplace_back(code);
    Log("violation code=" + std::string(code) + " risk=" + std::to_string(riskScore_));
}

int ViolationReporter::RiskScore() const { std::scoped_lock lock(mutex_); return riskScore_; }

std::string ViolationReporter::Codes() const
{
    std::scoped_lock lock(mutex_);
    std::string result;
    for (const auto& code : codes_)
    {
        if (!result.empty()) result += ',';
        result += code;
    }
    return result.empty() ? "NONE" : result;
}

bool ViolationReporter::ShouldDisconnect() const { return RiskScore() >= 80; }

bool SecureSession::Initialize()
{
#ifdef _WIN32
    sessionId_ = EnvironmentUtf8(L"ORIGINS_AC_SESSION");
    pipeName_ = EnvironmentUtf8(L"ORIGINS_AC_PIPE");
    expectedMainHash_ = EnvironmentUtf8(L"ORIGINS_AC_MAIN_HASH");
    const std::string encodedKey = EnvironmentUtf8(L"ORIGINS_AC_KEY");
    SetEnvironmentVariableW(L"ORIGINS_AC_KEY", nullptr);
    if (sessionId_.empty() || pipeName_.empty() || encodedKey.empty()) return false;
    key_.resize((encodedKey.size() * 3) / 4 + 3);
    const int decoded = EVP_DecodeBlock(key_.data(), reinterpret_cast<const unsigned char*>(encodedKey.data()), static_cast<int>(encodedKey.size()));
    if (decoded <= 0) return false;
    std::size_t padding = encodedKey.ends_with("==") ? 2 : (encodedKey.ends_with('=') ? 1 : 0);
    key_.resize(static_cast<std::size_t>(decoded) - padding);
    return key_.size() == 32;
#else
    return true;
#endif
}

bool SecureSession::IsValid() const { return !sessionId_.empty() && !key_.empty(); }

std::string SecureSession::Sign(std::string_view payload) const
{
    std::array<unsigned char, EVP_MAX_MD_SIZE> digest{};
    unsigned int length{};
    HMAC(EVP_sha256(), key_.data(), static_cast<int>(key_.size()), reinterpret_cast<const unsigned char*>(payload.data()), payload.size(), digest.data(), &length);
    return Hex(digest.data(), length);
}

void IntegrityManager::Initialize(const SecureSession& session, ViolationReporter& reporter)
{
    executablePath_ = ExecutablePath();
    expectedMainHash_ = session.ExpectedMainHash();
    initialTextHash_ = TextSectionHash();
    if (!expectedMainHash_.empty() && !EqualIgnoreCase(HashFile(executablePath_), expectedMainHash_)) reporter.Report("INTEGRITY_MAIN_FILE", 80);
    if (initialTextHash_.empty()) reporter.Report("INTEGRITY_TEXT_UNAVAILABLE", 5);
}

void IntegrityManager::Scan(ViolationReporter& reporter) const
{
    if (!expectedMainHash_.empty() && !EqualIgnoreCase(HashFile(executablePath_), expectedMainHash_)) reporter.Report("INTEGRITY_MAIN_FILE", 80);
    const auto current = TextSectionHash();
    if (!initialTextHash_.empty() && current != initialTextHash_) reporter.Report("INTEGRITY_CODE_SECTION", 80);
}

void ModuleScanner::Scan(ViolationReporter& reporter) const
{
#ifdef _WIN32
    const auto gameRoot = std::filesystem::weakly_canonical(ExecutablePath().parent_path()).wstring();
    std::array<wchar_t, MAX_PATH> windowsPath{};
    GetWindowsDirectoryW(windowsPath.data(), static_cast<UINT>(windowsPath.size()));
    const std::wstring windowsRoot = windowsPath.data();
    const std::unordered_set<std::wstring> allowedExternal = {
        L"gameoverlayrenderer64.dll", L"discordhook64.dll", L"nvspcap64.dll", L"rtsshooks64.dll"
    };
    const HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, GetCurrentProcessId());
    if (snapshot == INVALID_HANDLE_VALUE) return;
    MODULEENTRY32W module{sizeof(module)};
    if (Module32FirstW(snapshot, &module))
    {
        do
        {
            std::filesystem::path path(module.szExePath);
            std::wstring full = path.wstring();
            std::ranges::transform(full, full.begin(), [](wchar_t value) { return static_cast<wchar_t>(std::towlower(value)); });
            std::wstring game = gameRoot;
            std::ranges::transform(game, game.begin(), [](wchar_t value) { return static_cast<wchar_t>(std::towlower(value)); });
            std::wstring windows = windowsRoot;
            std::ranges::transform(windows, windows.begin(), [](wchar_t value) { return static_cast<wchar_t>(std::towlower(value)); });
            std::wstring name = path.filename().wstring();
            std::ranges::transform(name, name.begin(), [](wchar_t value) { return static_cast<wchar_t>(std::towlower(value)); });
            if (!full.starts_with(game) && !full.starts_with(windows) && !allowedExternal.contains(name))
            {
                reporter.Report("MODULE_OUTSIDE_ALLOWLIST", 10);
                break;
            }
        } while (Module32NextW(snapshot, &module));
    }
    CloseHandle(snapshot);
#endif
}

void AntiDebugManager::Scan(ViolationReporter& reporter) const
{
#ifdef _WIN32
    BOOL remote{};
    CheckRemoteDebuggerPresent(GetCurrentProcess(), &remote);
    if (IsDebuggerPresent() || remote) reporter.Report("DEBUGGER_ATTACHED", 40);
#endif
}

void SpeedValidation::Initialize()
{
    steadyStart_ = std::chrono::steady_clock::now();
    systemStart_ = std::chrono::system_clock::now();
}

void SpeedValidation::Tick(ViolationReporter& reporter)
{
    const auto steadyElapsed = std::chrono::duration<double>(std::chrono::steady_clock::now() - steadyStart_).count();
    const auto systemElapsed = std::chrono::duration<double>(std::chrono::system_clock::now() - systemStart_).count();
    if (steadyElapsed >= 10.0 && systemElapsed > 0.0)
    {
        const double ratio = steadyElapsed / systemElapsed;
        if (ratio < 0.80 || ratio > 1.20) reporter.Report("CLOCK_SPEED_MISMATCH", 20);
        Initialize();
    }
}

void SpeedValidation::RecordAttack(ViolationReporter& reporter)
{
    const auto now = std::chrono::steady_clock::now();
    if (lastAttack_ != std::chrono::steady_clock::time_point{} && now - lastAttack_ < std::chrono::milliseconds(8)) reporter.Report("ATTACK_INTERVAL_INVALID", 10);
    lastAttack_ = now;
}

HeartbeatClient::~HeartbeatClient()
{
#ifdef _WIN32
    if (pipe_ != INVALID_HANDLE_VALUE) CloseHandle(static_cast<HANDLE>(pipe_));
#endif
}

bool HeartbeatClient::Send(const SecureSession& session, const ViolationReporter& reporter)
{
#ifdef _WIN32
    if (pipe_ == INVALID_HANDLE_VALUE)
    {
        const std::wstring pipePath = L"\\\\.\\pipe\\" + std::wstring(session.PipeName().begin(), session.PipeName().end());
        if (!WaitNamedPipeW(pipePath.c_str(), 2000)) return false;
        pipe_ = CreateFileW(pipePath.c_str(), GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);
        if (pipe_ == INVALID_HANDLE_VALUE) return false;
    }
    const auto sequence = ++sequence_;
    std::string payload = session.SessionId() + '|' + std::to_string(sequence) + '|' + std::string(BuildId) + '|' + std::to_string(reporter.RiskScore()) + '|' + reporter.Codes();
    std::string message = payload + '|' + session.Sign(payload) + "\n";
    DWORD written{};
    if (!WriteFile(static_cast<HANDLE>(pipe_), message.data(), static_cast<DWORD>(message.size()), &written, nullptr) || written != message.size())
    {
        CloseHandle(static_cast<HANDLE>(pipe_));
        pipe_ = INVALID_HANDLE_VALUE;
        return false;
    }
#endif
    return true;
}

ClientAntiCheat& ClientAntiCheat::Instance() { static ClientAntiCheat instance; return instance; }

bool ClientAntiCheat::Initialize()
{
    if (!session_.Initialize())
    {
        reporter_.Report("SECURE_SESSION_MISSING", 100);
        return false;
    }
    integrity_.Initialize(session_, reporter_);
    speed_.Initialize();
    const auto now = std::chrono::steady_clock::now();
    nextFastScan_ = now;
    nextFullScan_ = now;
    if (!heartbeat_.Send(session_, reporter_)) reporter_.Report("HEARTBEAT_INITIAL_SEND_FAILED", 10);
    nextHeartbeat_ = now + std::chrono::seconds(15);
    return true;
}

bool ClientAntiCheat::Tick()
{
    const auto now = std::chrono::steady_clock::now();
    speed_.Tick(reporter_);
    if (now >= nextFastScan_)
    {
        antiDebug_.Scan(reporter_);
        nextFastScan_ = now + std::chrono::seconds(10);
    }
    if (now >= nextFullScan_)
    {
        integrity_.Scan(reporter_);
        modules_.Scan(reporter_);
        nextFullScan_ = now + std::chrono::seconds(60);
    }
    if (now >= nextHeartbeat_)
    {
        if (!heartbeat_.Send(session_, reporter_)) reporter_.Report("HEARTBEAT_SEND_FAILED", 10);
        nextHeartbeat_ = now + std::chrono::seconds(15);
    }
    return !reporter_.ShouldDisconnect();
}

void ClientAntiCheat::RecordAttack() { speed_.RecordAttack(reporter_); }
}
