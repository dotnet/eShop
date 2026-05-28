import unittest
import os
import sys
import traceback
from selenium import webdriver
from selenium.webdriver.chrome.service import Service as ChromeService
from selenium.webdriver.chrome.options import Options as ChromeOptions
from webdriver_manager.chrome import ChromeDriverManager

# Agregar la ruta raíz al path para importar correctamente los módulos locales
sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from tests_selenium.config import BASE_URL, USERNAME, PASSWORD, EVIDENCE_DIR
from tests_selenium.pages.login_page import LoginPage
from tests_selenium.pages.catalog_page import CatalogPage
from tests_selenium.pages.cart_page import CartPage
from tests_selenium.pages.base_page import logger

class TestFlujoCarrito(unittest.TestCase):
    """Pruebas funcionales de extremo a extremo para la gestión de productos y carrito de compras."""

    def setUp(self):
        logger.info("========================================= SETUP =========================================")
        logger.info("Inicializando el navegador Chrome para la prueba...")
        
        options = ChromeOptions()
        # Ignorar errores de certificados SSL locales autofirmados
        options.add_argument("--ignore-certificate-errors")
        options.add_argument("--allow-insecure-localhost")
        
        # Puedes habilitar el modo headless configurando la variable de entorno HEADLESS=true
        if os.getenv("HEADLESS", "false").lower() == "true":
            options.add_argument("--headless=new")
            options.add_argument("--disable-gpu")
        options.add_argument("--window-size=1920,1080")
        options.add_argument("--no-sandbox")
        options.add_argument("--disable-dev-shm-usage")
        
        # Configurar y descargar el driver de manera automática mediante WebDriver Manager
        service = ChromeService(ChromeDriverManager().install())
        self.driver = webdriver.Chrome(service=service, options=options)
        self.driver.implicitly_wait(2)
        
        # Instanciar las páginas del POM
        self.login_page = LoginPage(self.driver)
        self.catalog_page = CatalogPage(self.driver)
        self.cart_page = CartPage(self.driver)
        
        # URL de inicio
        self.login_page.open_url(BASE_URL)

    def test_gestion_carrito_y_remocion(self):
        """Verifica FR-01 y FR-02: Selección de producto, adición al carrito, actualización y remoción."""
        logger.info("-------------------- INICIANDO CASO DE PRUEBA: GESTIÓN DE CARRITO --------------------")
        
        producto_prueba = "Adventurer GPS Watch"
        evidencia_fr01_path = None
        evidencia_fr02_path = None
        
        try:
            # 1. Navegación en el catálogo como invitado (sin login previo)
            self.catalog_page.wait_for_catalog_loaded()
            self.catalog_page.select_product(producto_prueba)
            
            # 2. Verificar que un usuario no autenticado ve el botón "Log in to purchase" y no puede añadir directamente
            self.assertTrue(
                self.catalog_page.is_log_in_to_purchase_visible(),
                "Error: El botón 'Log in to purchase' debería estar visible para usuarios no autenticados."
            )
            self.catalog_page.capture_screenshot("evidencia_invitado_restriccion.png")
            logger.info("[INFO] Verificado: El usuario invitado tiene restringida la compra directa (FR-01).")
            
            # 3. Hacer clic en "Log in to purchase" para iniciar el flujo de autenticación guiado
            logger.info("Pulsando en 'Log in to purchase' para iniciar sesión...")
            self.catalog_page.click(self.catalog_page.LOG_IN_TO_PURCHASE_BUTTON)
            
            # 4. Completar inicio de sesión
            self.login_page.login(USERNAME, PASSWORD)
            self.assertTrue(
                self.login_page.is_logged_in_as(USERNAME),
                "Error crítico: El inicio de sesión falló tras la redirección."
            )
            
            # 5. Con la sesión iniciada, el sistema nos redirecciona automáticamente de regreso al detalle del producto.
            # Esperamos a que el botón "Add to shopping bag" aparezca en pantalla.
            logger.info("Esperando redirección automática al detalle del producto...")
            self.catalog_page.find_element(self.catalog_page.ADD_TO_BAG_BUTTON)
            
            # 6. Añadir el producto al carrito de compras (Ahora sí está habilitado el botón)
            self.catalog_page.add_to_shopping_bag()
            
            # 7. Ir a la pantalla de resumen del carrito y verificar presencia (FR-01)
            self.catalog_page.go_to_cart()
            self.cart_page.wait_for_cart_loaded()
            
            cantidad_inicial = self.cart_page.get_item_quantity()
            self.assertGreaterEqual(
                cantidad_inicial, 1,
                f"Error: La cantidad inicial en el carrito debería ser al menos 1, pero se encontró {cantidad_inicial}."
            )
            
            # Capturar evidencia de éxito para FR-01
            evidencia_fr01_path = self.cart_page.capture_screenshot("exito_FR-01.png")
            logger.info("[SUCCESS - FR-01] Producto correctamente añadido al carrito de compras.")
            
            # 8. Modificar cantidad a 0 para remover el artículo (FR-02)
            self.cart_page.update_item_quantity(0)
            
            # 9. Validar que aparezca el mensaje de carrito vacío
            self.assertTrue(
                self.cart_page.is_empty_message_visible(),
                "Error: No se visualizó el mensaje de 'Your shopping bag is empty' tras poner la cantidad en 0."
            )
            
            # Capturar evidencia de éxito para FR-02
            evidencia_fr02_path = self.cart_page.capture_screenshot("exito_FR-02.png")
            logger.info("[SUCCESS - FR-02] Producto correctamente removido del carrito. Carrito vacío detectado.")

        except Exception as e:
            logger.error("!!! ERROR DURANTE LA EJECUCIÓN DEL FLUJO DE CARRITO !!!")
            logger.error(traceback.format_exc())
            
            # Captura de pantalla de fallo en caso de excepción
            fallo_filename = "fallo_flujo_carrito.png"
            path_fallo = self.cart_page.capture_screenshot(fallo_filename)
            logger.error(f"[FAILURE] Evidencia de fallo almacenada en: {path_fallo}")
            
            # Relanzar el error para que unittest marque la prueba como fallida
            raise e

    def tearDown(self):
        logger.info("Cerrando el navegador...")
        if hasattr(self, 'driver'):
            self.driver.quit()
        logger.info("======================================= TEARDOWN =======================================")

if __name__ == "__main__":
    unittest.main()
