from selenium.webdriver.common.by import By
from tests_selenium.pages.base_page import BasePage, logger

class CatalogPage(BasePage):
    """Page Object para la página de catálogo y el detalle del producto."""
    
    # Locators
    CATALOG_CONTAINER = (By.CSS_SELECTOR, "div.catalog, div.catalog-items")
    ADD_TO_BAG_BUTTON = (By.CSS_SELECTOR, "button[title='Add to basket']")
    LOG_IN_TO_PURCHASE_BUTTON = (By.CSS_SELECTOR, "button[title='Log in to purchase']")
    CART_ICON_LINK = (By.CSS_SELECTOR, "a[aria-label='cart']")
    PRODUCT_ITEM_ANY = (By.CSS_SELECTOR, "a.catalog-product")
    
    def wait_for_catalog_loaded(self):
        """Espera a que el catálogo y sus productos se rendericen completamente en pantalla."""
        logger.info("Esperando a que el catálogo y los productos terminen de renderizarse...")
        self.is_visible(self.CATALOG_CONTAINER)
        # Esperar a que haya al menos un producto visible para asegurar que desapareció el "Loading..."
        return self.is_visible(self.PRODUCT_ITEM_ANY)

    def select_product(self, product_name):
        """Busca un producto por su nombre en el catálogo y hace clic en él."""
        logger.info(f"Seleccionando el producto '{product_name}' del catálogo...")
        # Localizador de XPath súper robusto que busca el enlace que contiene el texto del producto
        product_xpath = f"//a[contains(@class, 'catalog-product')][contains(., '{product_name}')]"
        product_locator = (By.XPATH, product_xpath)
        self.click(product_locator)
        
        # Esperar a que cargue el detalle (buscando el botón de agregar o de login para comprar)
        detail_element = (By.CSS_SELECTOR, "button[title='Add to basket'], button[title='Log in to purchase']")
        self.find_element(detail_element)
        logger.info(f"Detalle del producto '{product_name}' cargado.")

    def add_to_shopping_bag(self):
        """Hace clic en el botón 'Add to shopping bag' dentro del detalle y espera a que Blazor confirme la adición en el DOM."""
        logger.info("Añadiendo el producto al carrito de compras...")
        self.click(self.ADD_TO_BAG_BUTTON)
        
        # ESPERA ASÍNCRONA: Esperar a que el texto "in shopping bag" aparezca para confirmar persistencia.
        # Usamos "." en lugar de "text()" para acumular el texto de nodos hijos mezclados (strong, texto y a).
        confirmacion_locator = (By.XPATH, "//*[contains(., 'in shopping bag')]")
        self.find_element(confirmacion_locator)
        logger.info("Producto añadido y confirmado exitosamente en el DOM.")

    def is_log_in_to_purchase_visible(self):
        """Verifica si el botón de login para comprar está visible (usuario no autenticado)."""
        return self.is_visible(self.LOG_IN_TO_PURCHASE_BUTTON)

    def go_to_cart(self):
        """Hace clic en el icono del carrito en el Header para ir a la página de resumen."""
        logger.info("Navegando a la página del carrito desde el menú superior...")
        self.click(self.CART_ICON_LINK)
        # Esperar a que la URL cambie a '/cart'
        self.wait_for_url_contains("/cart")
        logger.info("Página de carrito cargada.")
