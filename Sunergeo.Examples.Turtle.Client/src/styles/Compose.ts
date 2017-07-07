import * as React from 'react';

export function compose(...args: (React.CSSProperties | undefined)[]): React.CSSProperties {
    // Defend against user error
    for (let obj of args) {
        if (obj instanceof Array) {
            throw new Error(`User error: use compose(a,b) instead of compose([a,b]). Object: ${obj}`)
        }
    }

    var newObj:React.CSSProperties = {};
    for (let obj of args) {
        if (obj) {
            for (let key in obj) {
                //copy all the fields
                if (obj[key] !== undefined) {
                    newObj[key] = obj[key];
                }
            }
        }
    }
    return newObj;
};
