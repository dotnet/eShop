package com.eshop.defaults;

import org.springframework.amqp.core.DirectExchange;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageDeliveryMode;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.boot.autoconfigure.AutoConfiguration;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Bean;

@AutoConfiguration
@ConditionalOnClass(RabbitTemplate.class)
public class EventBusAutoConfiguration {

    @Bean
    DirectExchange eshopEventBusExchange() {
        return EventBusConventions.exchange();
    }

    @Bean
    static org.springframework.beans.factory.config.BeanPostProcessor persistentMandatoryRabbitPublishing() {
        return new org.springframework.beans.factory.config.BeanPostProcessor() {
            @Override
            public Object postProcessAfterInitialization(Object bean, String beanName) {
                if (bean instanceof RabbitTemplate template) {
                    template.setMandatory(true);
                    template.setObservationEnabled(true);
                    template.addBeforePublishPostProcessors(EventBusAutoConfiguration::persistent);
                }
                return bean;
            }
        };
    }

    static Message persistent(Message message) {
        message.getMessageProperties().setDeliveryMode(MessageDeliveryMode.PERSISTENT);
        return message;
    }
}
