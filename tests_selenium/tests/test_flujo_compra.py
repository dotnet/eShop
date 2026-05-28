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

from tests_selenium.config import BASE_URL, USERNAME, PASSWORD
from tests_selenium.pages.login_page import LoginPage
from tests_selenium.pages.catalog_page import CatalogPage
from tests_selenium.pages.cart_page import CartPage
from tests_selenium.pages.checkout_page import CheckoutPage, BasketCheckoutInfo
from tests_selenium.pages.orders_page import OrdersPage
from tests_selenium.pages.base_page import logger

class TestFlujoCompra(unittest.TestCase):
    """Pruebas funcionales de extremo a extremo para la autenticación y proceso de compra completo."""

    def setUp(self):
        logger.info("========================================= SETUP =========================================")
        logger.info("Inicializando el navegador Chrome para la prueba...")
        
        options = ChromeOptions()
        # Ignorar errores de certificados SSL locales autofirmados
        options.add_argument("--ignore-certificate-errors")
        options.add_argument("--allow-insecure-localhost")
        
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
        self.checkout_page = CheckoutPage(self.driver)
        self.orders_page = OrdersPage(self.driver)
        
        # URL de inicio
        self.login_page.open_url(BASE_URL)

    def test_autenticacion_y_compra_completa(self):
        """Verifica FR-03 y FR-04: Login de usuario, selección, checkout de orden y validación en historial."""
        logger.info("-------------------- INICIANDO CASO DE PRUEBA: COMPRA COMPLETA (E2E) --------------------")
        
        producto_prueba = "Adventurer GPS Watch"
        datos_envio = BasketCheckoutInfo(
            street="Diagonal 45 # 23-44 Apt 302",
            city="Bogota D.C.",
            state="Bogota",
            zip_code="110221",
            country="Colombia"
        )
        
        try:
            # 1. Autenticación (FR-03)
            self.login_page.click_sign_in()
            self.login_page.login(USERNAME, PASSWORD)
            
            # Validar login exitoso
            self.assertTrue(
                self.login_page.is_logged_in_as(USERNAME),
                f"Error: El inicio de sesión para el usuario '{USERNAME}' falló."
            )
            
            # Capturar evidencia de éxito para FR-03
            self.login_page.capture_screenshot("exito_FR-03.png")
            logger.info("[SUCCESS - FR-03] Autenticación de cliente exitosa. Sesión activa para 'bob'.")

            # 2. Navegación al catálogo y selección de producto
            self.catalog_page.wait_for_catalog_loaded()
            self.catalog_page.select_product(producto_prueba)
            
            # 3. Añadir el producto al carrito de compras
            self.catalog_page.add_to_shopping_bag()
            
            # 4. Ir a la pantalla de resumen del carrito
            self.catalog_page.go_to_cart()
            self.cart_page.wait_for_cart_loaded()
            
            # 5. Ir a la pantalla de Checkout (FR-04)
            self.cart_page.click_checkout()
            self.checkout_page.wait_for_checkout_loaded()
            
            # 6. Rellenar los datos de envío y colocar el pedido
            self.checkout_page.fill_shipping_address(
                street=datos_envio.street,
                city=datos_envio.city,
                state=datos_envio.state,
                zip_code=datos_envio.zip_code,
                country=datos_envio.country
            )
            self.checkout_page.place_order()
            
            # 7. Validar redirección automática al historial de órdenes y verificar creación
            self.orders_page.wait_for_orders_loaded()
            
            # Comprobar que haya al menos una orden listada
            cantidad_ordenes = self.orders_page.get_orders_count()
            self.assertGreater(
                cantidad_ordenes, 0,
                "Error: No se encontró ningún registro de orden en el historial del usuario."
            )
            
            # Obtener y validar el estado de la última orden creada
            detalles_orden = self.orders_page.get_latest_order_details()
            self.assertNotEqual(
                detalles_orden["number"], "",
                "Error: El número de la orden más reciente no se visualizó correctamente."
            )
            
            # Capturar evidencia de éxito para FR-04
            self.orders_page.capture_screenshot("exito_FR-04.png")
            logger.info(f"[SUCCESS - FR-04] Compra completada de extremo a extremo. Orden Nro: {detalles_orden['number']} creada con estado: '{detalles_orden['status']}'.")

        except Exception as e:
            logger.error("!!! ERROR DURANTE LA EJECUCIÓN DEL FLUJO DE COMPRA !!!")
            logger.error(traceback.format_exc())
            
            # Captura de pantalla de fallo en caso de excepción
            fallo_filename = "fallo_flujo_compra.png"
            path_fallo = self.orders_page.capture_screenshot(fallo_filename)
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
