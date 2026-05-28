import os
import platform
from typing import Optional

from webdriver_manager.core.download_manager import DownloadManager
from webdriver_manager.core.driver_cache import DriverCacheManager
from webdriver_manager.core.manager import DriverManager
from webdriver_manager.core.os_manager import OperationSystemManager
from webdriver_manager.drivers.firefox import GeckoDriver


class GeckoDriverManager(DriverManager):
    def __init__(
            self,
            version: Optional[str] = None,
            name: str = "geckodriver",
            url: str = "https://github.com/mozilla/geckodriver/releases/download",
            latest_release_url: str = "https://api.github.com/repos/mozilla/geckodriver/releases/latest",
            mozila_release_tag: str = "https://api.github.com/repos/mozilla/geckodriver/releases/tags/{0}",
            download_manager: Optional[DownloadManager] = None,
            cache_manager: Optional[DriverCacheManager] = None,
            os_system_manager: Optional[OperationSystemManager] = None
    ):
        super(GeckoDriverManager, self).__init__(
            download_manager=download_manager,
            cache_manager=cache_manager,
            os_system_manager=os_system_manager
        )

        self.driver = GeckoDriver(
            driver_version=version,
            name=name,
            url=url,
            latest_release_url=latest_release_url,
            mozila_release_tag=mozila_release_tag,
            http_client=self.http_client,
            os_system_manager=os_system_manager
        )

    def install(self) -> str:
        driver_path = self._get_driver_binary_path(self.driver)
        os.chmod(driver_path, 0o755)
        return driver_path

    def get_os_type(self):
        os_type = super().get_os_type()
        if self._is_linux_aarch64():
            return "linux-aarch64"
        if self._os_system_manager.is_arch():
            return f'{self._os_system_manager.get_os_name()}{"os" if self._os_system_manager.is_mac_os(os_type) else ""}-aarch{self._os_system_manager.get_os_architecture()}'
        if self._os_system_manager.is_mac_os(os_type):
            return 'macos'
        return os_type

    def _is_linux_aarch64(self) -> bool:
        if self._os_system_manager.get_os_name() != "linux":
            return False

        machine_candidates = [platform.machine(), platform.uname().machine, platform.processor()]
        try:
            machine_candidates.append(os.uname().machine)
        except AttributeError:
            pass

        machine_info = " ".join(value.lower() for value in machine_candidates if value)
        return any(arch in machine_info for arch in ("aarch64", "arm64"))
