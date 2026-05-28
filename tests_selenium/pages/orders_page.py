from selenium.webdriver.common.by import By
from tests_selenium.pages.base_page import BasePage, logger

class OrdersPage(BasePage):
    """Page Object para la sección de historial de órdenes del usuario."""
    
    # Locators
    ORDERS_HEADER = (By.XPATH, "//h1[contains(text(), 'Orders')] | //*[contains(@class, 'page-header-title') and contains(text(), 'Orders')]")
    ORDERS_LIST = (By.CSS_SELECTOR, "ul.orders-list")
    ORDER_ITEMS = (By.CSS_SELECTOR, "li.orders-item:not(.orders-header)")
    
    # Locators específicos de la última orden (primera en la lista después del header)
    LATEST_ORDER_NUMBER = (By.CSS_SELECTOR, "li.orders-item:not(.orders-header) div.order-number")
    LATEST_ORDER_TOTAL = (By.CSS_SELECTOR, "li.orders-item:not(.orders-header) div.order-total")
    LATEST_ORDER_STATUS = (By.CSS_SELECTOR, "li.orders-item:not(.orders-header) div.order-status span")
    
    def wait_for_orders_loaded(self):
        """Espera a que cargue la página del historial de órdenes."""
        logger.info("Esperando a que cargue la pantalla de órdenes...")
        return self.is_visible(self.ORDERS_HEADER)

    def get_orders_count(self):
        """Retorna la cantidad de órdenes encontradas en el historial."""
        items = self.find_elements(self.ORDER_ITEMS)
        count = len(items)
        logger.info(f"Número de órdenes detectadas en la lista: {count}")
        return count

    def get_latest_order_details(self):
        """Obtiene el número, total y estado de la orden más reciente."""
        logger.info("Obteniendo detalles de la orden más reciente...")
        
        order_number = self.get_text(self.LATEST_ORDER_NUMBER).strip()
        order_total = self.get_text(self.LATEST_ORDER_TOTAL).strip()
        order_status = self.get_text(self.LATEST_ORDER_STATUS).strip()
        
        logger.info(f"Detalles de la última orden: Nro={order_number}, Total={order_total}, Estado={order_status}")
        return {
            "number": order_number,
            "total": order_total,
            "status": order_status
        }
