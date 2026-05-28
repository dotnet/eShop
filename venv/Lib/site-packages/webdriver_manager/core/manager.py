from webdriver_manager.core.download_manager import WDMDownloadManager
from webdriver_manager.core.driver_cache import DriverCacheManager
from webdriver_manager.core.logger import log
from webdriver_manager.core.os_manager import OperationSystemManager
import os
import time


class DriverManager(object):
    def __init__(
            self,
            download_manager=None,
            cache_manager=None,
            os_system_manager=None
    ):
        self._os_system_manager = os_system_manager
        if not self._os_system_manager:
            self._os_system_manager = OperationSystemManager()
        self._cache_manager = cache_manager
        if not self._cache_manager:
            self._cache_manager = DriverCacheManager(os_system_manager=self._os_system_manager)

        self._download_manager = download_manager
        if self._download_manager is None:
            self._download_manager = WDMDownloadManager()


        log("====== WebDriver manager ======")

    @property
    def http_client(self):
        return self._download_manager.http_client

    def install(self) -> str:
        raise NotImplementedError("Please Implement this method")

    def _get_driver_binary_path(self, driver):
        binary_path = self._cache_manager.find_driver(driver)
        if binary_path:
            return binary_path

        os_type = self.get_os_type()
        lock_path = self._cache_manager.get_driver_lock_path(driver.get_name(), os_type)
        lock_fd = self._acquire_lock(lock_path)
        try:
            # Re-check cache after lock to avoid duplicate downloads in concurrent runs.
            binary_path = self._cache_manager.find_driver(driver)
            if binary_path:
                return binary_path

            file = self._download_manager.download_file(driver.get_driver_download_url(os_type))
            binary_path = self._cache_manager.save_file_to_cache(driver, file)
            return binary_path
        finally:
            self._release_lock(lock_fd, lock_path)

    def get_os_type(self):
        return self._os_system_manager.get_os_type()

    @staticmethod
    def _acquire_lock(lock_path: str, timeout: float = 60.0, poll_interval: float = 0.1):
        start = time.time()
        while True:
            try:
                return os.open(lock_path, os.O_CREAT | os.O_EXCL | os.O_RDWR)
            except FileExistsError:
                if time.time() - start >= timeout:
                    raise TimeoutError(f"Timed out waiting for webdriver-manager lock: {lock_path}")
                time.sleep(poll_interval)

    @staticmethod
    def _release_lock(lock_fd, lock_path: str):
        if lock_fd is not None:
            os.close(lock_fd)
        try:
            os.unlink(lock_path)
        except FileNotFoundError:
            pass
