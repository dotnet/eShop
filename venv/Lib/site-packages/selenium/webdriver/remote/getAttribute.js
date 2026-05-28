(function () {
    const PROPERTY_ALIASES = {
        'readonly': 'readOnly',
    };
    const BOOLEAN_PROPERTIES = [
        'allowfullscreen',
        'allowpaymentrequest',
        'allowusermedia',
        'async',
        'autofocus',
        'autoplay',
        'checked',
        'compact',
        'complete',
        'controls',
        'declare',
        'default',
        'defaultchecked',
        'defaultselected',
        'defer',
        'disabled',
        'ended',
        'formnovalidate',
        'hidden',
        'indeterminate',
        'iscontenteditable',
        'ismap',
        'itemscope',
        'loop',
        'multiple',
        'muted',
        'nohref',
        'nomodule',
        'noresize',
        'noshade',
        'novalidate',
        'nowrap',
        'open',
        'paused',
        'playsinline',
        'pubdate',
        'readonly',
        'required',
        'reversed',
        'scoped',
        'seamless',
        'seeking',
        'selected',
        'truespeed',
        'typemustmatch',
        'willvalidate',
    ];
    function getAttribute(element, attributeName) {
        return element.getAttribute(attributeName.toLowerCase());
    }
    function getProperty(element, propertyName) {
        return element[propertyName];
    }
    function isElement(node, tagName) {
        const elem = node;
        if (!elem || elem.nodeType !== 1) {
            return false;
        }
        if (!tagName) {
            return true;
        }
        const normalizedTag = tagName.toUpperCase();
        if (node instanceof HTMLFormElement) {
            return normalizedTag === 'FORM';
        }
        return typeof elem.tagName === 'string' && elem.tagName.toUpperCase() === normalizedTag;
    }
    function isSelectable(element) {
        if (isElement(element, 'OPTION')) {
            return true;
        }
        if (isElement(element, 'INPUT')) {
            const type = element.type.toLowerCase();
            return type === 'checkbox' || type === 'radio';
        }
        return false;
    }
    function isSelected(element) {
        var _a;
        if (isElement(element, 'OPTION')) {
            return element.selected;
        }
        const type = (_a = element.type) === null || _a === void 0 ? void 0 : _a.toLowerCase();
        if (type === 'checkbox' || type === 'radio') {
            return element.checked;
        }
        return false;
    }
    function isObject(value) {
        return value !== null && (typeof value === 'object' || typeof value === 'function');
    }
    return function get(element, attribute) {
        const name = attribute.toLowerCase();
        if (name === 'style') {
            const style = element.style;
            if (!style) {
                return null;
            }
            return typeof style === 'string' ? style : style.cssText;
        }
        if ((name === 'selected' || name === 'checked') && isSelectable(element)) {
            return isSelected(element) ? 'true' : null;
        }
        const isLink = isElement(element, 'A');
        const isImg = isElement(element, 'IMG');
        if ((isImg && name === 'src') || (isLink && name === 'href')) {
            const attrValue = getAttribute(element, name);
            if (attrValue) {
                return String(getProperty(element, name));
            }
            return attrValue;
        }
        if (name === 'spellcheck') {
            const attrValue = getAttribute(element, name);
            if (attrValue !== null) {
                const lower = attrValue.toLowerCase();
                if (lower === 'false') {
                    return 'false';
                }
                if (lower === 'true') {
                    return 'true';
                }
            }
            return String(getProperty(element, name));
        }
        const propName = PROPERTY_ALIASES[attribute] || attribute;
        if (BOOLEAN_PROPERTIES.includes(name)) {
            const hasAttr = getAttribute(element, attribute) !== null;
            const propValue = getProperty(element, propName);
            return hasAttr || !!propValue ? 'true' : null;
        }
        if (name === 'value' && isElement(element, 'LI')) {
            const attrValue = getAttribute(element, attribute);
            return attrValue != null ? attrValue : null;
        }
        let property;
        try {
            property = getProperty(element, propName);
        }
        catch (_e) {
        }
        if (property == null || isObject(property)) {
            const attrValue = getAttribute(element, attribute);
            return attrValue != null ? attrValue : null;
        }
        return property != null ? String(property) : null;
    };
})()