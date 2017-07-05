import * as _ from 'lodash';

// CSS.supports() Polyfill per https://developer.mozilla.org/en-US/docs/Web/API/CSS/supports
// Original idea from https://gist.github.com/codler/03a0995195aa2859465f
if (!('CSS' in window)) {
    (window as any).CSS = {};
}

if (!('supports' in (window as any).CSS)) {
    const cacheSupports:{[key:string]:boolean} = {};

    (window as any).CSS.supports = (propertyName:string, value?:string) => {

        const innerCssSupports = (s:string):boolean => {
            if (s in cacheSupports) {
                return cacheSupports[s];
            }

            const style:CSSStyleDeclaration = document.createElement('div').style;

            let success = true;
            let hasNestedConditions = false;

            if (success) {
                const orParts = s.split(/\)or\(/i);
                if (orParts.length > 1) {
                    hasNestedConditions = true;
                    success = _.some(orParts, innerCssSupports);
                }
            }

            if (success) {
                const andParts = s.split(/\)and\(/i);
                if (andParts.length > 1) {
                    hasNestedConditions = true;
                    success = _.every(andParts, innerCssSupports);
                }
            }

            return cacheSupports[s] = 
                (():boolean => {
                    if (hasNestedConditions) {
                        return success;
                    }
                    else {
                        // Remove the first and last parentheses
                        style.cssText = s.replace(/^\(/, '').replace(/\)$/, '');
                        return !!style.length;
                    }
                })()
        }

        const propertyNameAndValue = (value ? (`${propertyName}:${value}`) : propertyName).replace(/ /g, '');
        const result = innerCssSupports(propertyNameAndValue);
        return result;
    };
}