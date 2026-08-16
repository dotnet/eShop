package com.eshop.defaults;

import com.eshop.integrationevents.ProductPriceChangedIntegrationEvent;
import org.junit.jupiter.api.Test;
import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.DirectExchange;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageDeliveryMode;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.rabbit.connection.CachingConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class EventBusConventionsTest {

    @Test
    void topologyIsDurableDirectAndUsesSimpleEventClassName() {
        DirectExchange exchange = EventBusConventions.exchange();
        Queue queue = EventBusConventions.durableQueue("catalog-api");
        Binding binding = EventBusConventions.bind(
                queue, exchange, ProductPriceChangedIntegrationEvent.class);

        assertEquals("eshop_event_bus", exchange.getName());
        assertTrue(exchange.isDurable());
        assertFalse(exchange.isAutoDelete());
        assertTrue(queue.isDurable());
        assertEquals("ProductPriceChangedIntegrationEvent", binding.getRoutingKey());
    }

    @Test
    void publisherIsMandatory() {
        RabbitTemplate template = new RabbitTemplate(new CachingConnectionFactory());
        EventBusAutoConfiguration.persistentMandatoryRabbitPublishing()
                .postProcessAfterInitialization(template, "rabbitTemplate");

        Message message = new Message(new byte[0], new MessageProperties());
        assertTrue(template.isMandatoryFor(message));
        assertEquals(MessageDeliveryMode.PERSISTENT,
                EventBusAutoConfiguration.persistent(message).getMessageProperties().getDeliveryMode());
        template.stop();
    }
}
