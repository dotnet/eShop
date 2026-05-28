import platform
import sys

from webdriver_manager.core.utils import linux_browser_apps_to_cmd, windows_browser_apps_to_cmd, \
    read_version_from_cmd


class ChromeType(object):
    GOOGLE = "google-chrome"
    CHROMIUM = "chromium"
    BRAVE = "brave-browser"
    MSEDGE = "edge"


class OSType(object):
    LINUX = "linux"
    MAC = "mac"
    WIN = "win"


PATTERN = {
    ChromeType.CHROMIUM: r"\d+\.\d+\.\d+",
    ChromeType.GOOGLE: r"\d+\.\d+\.\d+",
    ChromeType.MSEDGE: r"\d+\.\d+\.\d+",
    ChromeType.BRAVE: r"\d+\.\d+\.\d+(\.\d+)?",
    "firefox": r"(\d+.\d+)",
}


class OperationSystemManager(object):

    def __init__(self, os_type=None, architecture=None, os_platform=None):
        self._os_type = os_type
        if architecture is None and os_platform is None and os_type is not None:
            os_platform = os_type
            architecture = os_type
        self._os_platform = os_platform
        self._architecture = architecture

    def get_os_name(self):
        pl = sys.platform if self._os_platform is None else self._os_platform
        if pl in ["linux", "linux2", "linux32", "linux64"]:
            return OSType.LINUX
        elif pl in ["darwin","mac","mac64", 'mac64_m1']:
            return OSType.MAC
        elif pl in ["win32", "cygwin", "win64"]:
            return OSType.WIN

    def get_os_architecture(self):
        if self._architecture is not None:
            if self._architecture.endswith("64") or self._architecture.endswith("64_m1"):
                return 64
            else:
                return 32
        if platform.machine().endswith("64"):
            return 64
        else:
            return 32

    def get_os_type(self):
        return f"{self.get_os_name()}{'-arm' if self.is_arch() else ''}{self.get_os_architecture()}"

    def is_arch(self):
        machine = platform.machine() if self._architecture is None else self._architecture
        return any(arch in machine for arch in ["arm", "aarch64", "arm64", "armv7l", "armv8", "armv6", 'mac64_m1'])

    @staticmethod
    def is_mac_os(os_sys_type):
        return OSType.MAC in os_sys_type

    def get_browser_version_from_os(self, browser_type=None):
        """Return installed browser version."""
        cmd_mapping = {
            ChromeType.GOOGLE: {
                OSType.LINUX: linux_browser_apps_to_cmd(
                    "google-chrome",
                    "google-chrome-stable",
                    "google-chrome-beta",
                    "google-chrome-dev",
                ),
                OSType.MAC: r"/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --version",
                OSType.WIN: windows_browser_apps_to_cmd(
                    r'(Get-Item -Path "$env:PROGRAMFILES\Google\Chrome\Application\chrome.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:PROGRAMFILES (x86)\Google\Chrome\Application\chrome.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe").VersionInfo.FileVersion',
                    r'(Get-ItemProperty -Path Registry::"HKCU\SOFTWARE\Google\Chrome\BLBeacon").version',
                    r'(Get-ItemProperty -Path Registry::"HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome").version',
                ),
            },
            ChromeType.CHROMIUM: {
                OSType.LINUX: linux_browser_apps_to_cmd("chromium", "chromium-browser"),
                OSType.MAC: r"/Applications/Chromium.app/Contents/MacOS/Chromium --version",
                OSType.WIN: windows_browser_apps_to_cmd(
                    r'(Get-Item -Path "$env:PROGRAMFILES\Chromium\Application\chrome.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:PROGRAMFILES (x86)\Chromium\Application\chrome.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:LOCALAPPDATA\Chromium\Application\chrome.exe").VersionInfo.FileVersion',
                    r'(Get-ItemProperty -Path Registry::"HKCU\SOFTWARE\Chromium\BLBeacon").version',
                    r'(Get-ItemProperty -Path Registry::"HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Chromium").version',
                ),
            },
            ChromeType.BRAVE: {
                OSType.LINUX: linux_browser_apps_to_cmd(
                    "brave-browser", "brave-browser-beta", "brave-browser-nightly"
                ),
                OSType.MAC: r"/Applications/Brave\ Browser.app/Contents/MacOS/Brave\ Browser --version",
                OSType.WIN: windows_browser_apps_to_cmd(
                    r'(Get-Item -Path "$env:PROGRAMFILES\BraveSoftware\Brave-Browser\Application\brave.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:PROGRAMFILES (x86)\BraveSoftware\Brave-Browser\Application\brave.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\Application\brave.exe").VersionInfo.FileVersion',
                    r'(Get-ItemProperty -Path Registry::"HKCU\SOFTWARE\BraveSoftware\Brave-Browser\BLBeacon").version',
                    r'(Get-ItemProperty -Path Registry::"HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\BraveSoftware Brave-Browser").version',
                ),
            },
            ChromeType.MSEDGE: {
                OSType.LINUX: linux_browser_apps_to_cmd(
                    "microsoft-edge",
                    "microsoft-edge-stable",
                    "microsoft-edge-beta",
                    "microsoft-edge-dev",
                ),
                OSType.MAC: r"/Applications/Microsoft\ Edge.app/Contents/MacOS/Microsoft\ Edge --version",
                OSType.WIN: windows_browser_apps_to_cmd(
                    # stable edge
                    r'(Get-Item -Path "$env:PROGRAMFILES\Microsoft\Edge\Application\msedge.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:PROGRAMFILES (x86)\Microsoft\Edge\Application\msedge.exe").VersionInfo.FileVersion',
                    r'(Get-ItemProperty -Path Registry::"HKCU\SOFTWARE\Microsoft\Edge\BLBeacon").version',
                    r'(Get-ItemProperty -Path Registry::"HKLM\SOFTWARE\Microsoft\EdgeUpdate\Clients\{56EB18F8-8008-4CBD-B6D2-8C97FE7E9062}").pv',
                    # beta edge
                    r'(Get-Item -Path "$env:LOCALAPPDATA\Microsoft\Edge Beta\Application\msedge.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:PROGRAMFILES\Microsoft\Edge Beta\Application\msedge.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:PROGRAMFILES (x86)\Microsoft\Edge Beta\Application\msedge.exe").VersionInfo.FileVersion',
                    r'(Get-ItemProperty -Path Registry::"HKCU\SOFTWARE\Microsoft\Edge Beta\BLBeacon").version',
                    # dev edge
                    r'(Get-Item -Path "$env:LOCALAPPDATA\Microsoft\Edge Dev\Application\msedge.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:PROGRAMFILES\Microsoft\Edge Dev\Application\msedge.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:PROGRAMFILES (x86)\Microsoft\Edge Dev\Application\msedge.exe").VersionInfo.FileVersion',
                    r'(Get-ItemProperty -Path Registry::"HKCU\SOFTWARE\Microsoft\Edge Dev\BLBeacon").version',
                    # canary edge
                    r'(Get-Item -Path "$env:LOCALAPPDATA\Microsoft\Edge SxS\Application\msedge.exe").VersionInfo.FileVersion',
                    r'(Get-ItemProperty -Path Registry::"HKCU\SOFTWARE\Microsoft\Edge SxS\BLBeacon").version',
                    # highest edge
                    r"(Get-Item (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe').'(Default)').VersionInfo.ProductVersion",
                    r"[System.Diagnostics.FileVersionInfo]::GetVersionInfo((Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe').'(Default)').ProductVersion",
                    r"Get-AppxPackage -Name *MicrosoftEdge.* | Foreach Version",
                    r'(Get-ItemProperty -Path Registry::"HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge").version',
                ),
            },
            "firefox": {
                OSType.LINUX: linux_browser_apps_to_cmd("firefox"),
                OSType.MAC: r"/Applications/Firefox.app/Contents/MacOS/firefox --version",
                OSType.WIN: windows_browser_apps_to_cmd(
                    r'(Get-Item -Path "$env:PROGRAMFILES\Mozilla Firefox\firefox.exe").VersionInfo.FileVersion',
                    r'(Get-Item -Path "$env:PROGRAMFILES (x86)\Mozilla Firefox\firefox.exe").VersionInfo.FileVersion',
                    r"(Get-Item (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe').'(Default)').VersionInfo.ProductVersion",
                    r'(Get-ItemProperty -Path Registry::"HKLM\SOFTWARE\Mozilla\Mozilla Firefox").CurrentVersion',
                ),
            },
        }

        try:
            cmd_mapping = cmd_mapping[browser_type][self.get_os_name()]
            pattern = PATTERN[browser_type]
            version = read_version_from_cmd(cmd_mapping, pattern)
            return version
        except Exception:
            return None
            # raise Exception("Can not get browser version from OS")
