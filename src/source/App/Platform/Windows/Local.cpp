// Local.cpp: implementation of the Local
//////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "App/Platform/Windows/Local.h"

bool CheckSpecialText(wchar_t* Text)
{
    for (auto* lpszCheck = (wchar_t*)Text; *lpszCheck; ++lpszCheck)
    {
        // Keep the legacy ASCII restrictions for punctuation, but allow Unicode
        // letters/digits (including Thai) so names can be entered in any locale.
        // The server remains the authority for the final character-name policy.
        if (*lpszCheck < 0x20 ||
            (!(48 <= *lpszCheck && *lpszCheck < 58) && !(65 <= *lpszCheck && *lpszCheck < 91) &&
             !(97 <= *lpszCheck && *lpszCheck < 123) && *lpszCheck < 0x80))
        {
            return true;
        }
    }

    return false;
}
