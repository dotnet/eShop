import os
import logging
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By
from tests_selenium.config import DEFAULT_TIMEOUT, EVIDENCE_DIR

# Configurar el logger
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class BasePage:
    """Clase base de Page Object Model para encapsular esperas explícitas e interacciones comunes."""
    
    def __init__(self, driver):
        self.driver = driver
        self.timeout = DEFAULT_TIMEOUT

    def open_url(self, url):
        """Abre una URL y registra el evento."""
        logger.info(f"Navegando a la URL: {url}")
        self.driver.get(url)

    def find_element(self, locator, timeout=None):
        """Espera explícitamente a que un elemento esté presente en el DOM y visible en pantalla."""
        t = timeout or self.timeout
        try:
            element = WebDriverWait(self.driver, t).until(
                EC.visibility_of_element_located(locator)
            )
            return element
        except Exception as e:
            logger.error(f"Elemento no visible tras {t}s: {locator}")
            raise e

    def find_elements(self, locator, timeout=None):
        """Espera a que al menos un elemento esté presente y visible."""
        t = timeout or self.timeout
        try:
            elements = WebDriverWait(self.driver, t).until(
                EC.presence_of_all_elements_located(locator)
            )
            return elements
        except Exception as e:
            logger.error(f"Elementos no encontrados tras {t}s: {locator}")
            return []

    def click(self, locator, timeout=None):
        """Espera explícitamente a que un elemento sea clicable y realiza el clic."""
        t = timeout or self.timeout
        try:
            element = WebDriverWait(self.driver, t).until(
                EC.element_to_be_clickable(locator)
            )
            logger.info(f"Haciendo clic en el elemento: {locator}")
            element.click()
        except Exception as e:
            logger.error(f"No se pudo hacer clic en el elemento tras {t}s: {locator}")
            raise e

    def fill(self, locator, text, timeout=None):
        """Espera a que un campo de entrada sea visible, lo limpia y escribe el texto."""
        t = timeout or self.timeout
        try:
            element = self.find_element(locator, t)
            logger.info(f"Escribiendo texto en {locator}")
            element.clear()
            element.send_keys(text)
        except Exception as e:
            logger.error(f"No se pudo escribir en el elemento tras {t}s: {locator}")
            raise e

    def get_text(self, locator, timeout=None):
        """Obtiene el texto de un elemento visible."""
        element = self.find_element(locator, timeout)
        return element.text

    def is_visible(self, locator, timeout=None):
        """Verifica si un elemento es visible y retorna True o False."""
        t = timeout or self.timeout
        try:
            WebDriverWait(self.driver, t).until(
                EC.visibility_of_element_located(locator)
            )
            return True
        except Exception:
            return False

    def wait_for_url_contains(self, substring, timeout=None):
        """Espera a que la URL del navegador contenga una subcadena."""
        t = timeout or self.timeout
        try:
            WebDriverWait(self.driver, t).until(
                EC.url_contains(substring)
            )
            logger.info(f"URL ahora contiene: {substring}")
            return True
        except Exception as e:
            logger.error(f"La URL '{self.driver.current_url}' no contuvo '{substring}' tras {t}s")
            raise e

    def wait_for_text_to_be_present(self, locator, text, timeout=None):
        """Espera a que el texto esté presente en el elemento especificado."""
        t = timeout or self.timeout
        try:
            WebDriverWait(self.driver, t).until(
                EC.text_to_be_present_in_element(locator, text)
            )
            return True
        except Exception as e:
            logger.error(f"El texto '{text}' no apareció en el elemento {locator} tras {t}s")
            raise e

    def capture_screenshot(self, filename):
        """Guarda una captura de pantalla en el directorio de evidencias."""
        path = os.path.join(EVIDENCE_DIR, filename)
        try:
            self.driver.save_screenshot(path)
            logger.info(f"Captura de pantalla guardada exitosamente en: {path}")
            return path
        except Exception as e:
            logger.error(f"Error al guardar la captura de pantalla: {e}")
            return None
