package com.eshop.defaults;

import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.DirectExchange;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.core.QueueBuilder;

import java.util.Objects;

public final class EventBusConventions {
    public static final String EXCHANGE_NAME = "eshop_event_bus";

    private EventBusConventions() {
    }

    public static String routingKey(Class<?> eventType) {
        return Objects.requireNonNull(eventType, "eventType").getSimpleName();
    }

    public static DirectExchange exchange() {
        return new DirectExchange(EXCHANGE_NAME, true, false);
    }

    public static Queue durableQueue(String queueName) {
        return QueueBuilder.durable(Objects.requireNonNull(queueName, "queueName")).build();
    }

    public static Binding bind(Queue queue, DirectExchange exchange, Class<?> eventType) {
        return BindingBuilder.bind(queue).to(exchange).with(routingKey(eventType));
    }
}
