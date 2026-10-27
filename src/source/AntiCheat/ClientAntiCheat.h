// User-mode Origins anti-cheat. It inspects only this process and its loaded modules.
#pragma once

#include <chrono>
#include <cstdint>
#include <filesystem>
#include <mutex>
#include <string>
#include <string_view>
#include <unordered_set>
#include <vector>

namespace Origins::AntiCheat
{
class ViolationReporter
{
public:
    void Report(std::string_view code, int points);
    [[nodiscard]] int RiskScore() const;
    [[nodiscard]] std::string Codes() const;
    [[nodiscard]] bool ShouldDisconnect() const;

private:
    mutable std::mutex mutex_;
    int riskScore_{};
    std::vector<std::string> codes_;
};

class SecureSession
{
public:
    bool Initialize();
    [[nodiscard]] bool IsValid() const;
    [[nodiscard]] std::string Sign(std::string_view payload) const;
    [[nodiscard]] const std::string& SessionId() const { return sessionId_; }
    [[nodiscard]] const std::string& PipeName() const { return pipeName_; }
    [[nodiscard]] const std::string& ExpectedMainHash() const { return expectedMainHash_; }

private:
    std::string sessionId_;
    std::string pipeName_;
    std::string expectedMainHash_;
    std::vector<unsigned char> key_;
};

class IntegrityManager
{
public:
    void Initialize(const SecureSession& session, ViolationReporter& reporter);
    void Scan(ViolationReporter& reporter) const;

private:
    std::string initialTextHash_;
    std::string expectedMainHash_;
    std::filesystem::path executablePath_;
};

class ModuleScanner
{
public:
    void Scan(ViolationReporter& reporter) const;
};

class AntiDebugManager
{
public:
    void Scan(ViolationReporter& reporter) const;
};

class SpeedValidation
{
public:
    void Initialize();
    void Tick(ViolationReporter& reporter);
    void RecordAttack(ViolationReporter& reporter);

private:
    std::chrono::steady_clock::time_point steadyStart_{};
    std::chrono::system_clock::time_point systemStart_{};
    std::chrono::steady_clock::time_point lastAttack_{};
};

class HeartbeatClient
{
public:
    ~HeartbeatClient();
    bool Send(const SecureSession& session, const ViolationReporter& reporter);

private:
    std::uint64_t sequence_{};
#ifdef _WIN32
    void* pipe_{reinterpret_cast<void*>(-1)};
#endif
};

class ClientAntiCheat
{
public:
    static ClientAntiCheat& Instance();
    bool Initialize();
    bool Tick();
    void RecordAttack();

private:
    SecureSession session_;
    ViolationReporter reporter_;
    IntegrityManager integrity_;
    ModuleScanner modules_;
    AntiDebugManager antiDebug_;
    SpeedValidation speed_;
    HeartbeatClient heartbeat_;
    std::chrono::steady_clock::time_point nextFastScan_{};
    std::chrono::steady_clock::time_point nextFullScan_{};
    std::chrono::steady_clock::time_point nextHeartbeat_{};
};
}
