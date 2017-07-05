import * as React from 'react';
import {compose} from './Compose';

/**
 * Box helpers
 * Having top, left, bottom, right seperated makes it easier to override and maintain individual properties
 */
export namespace Box {
    /**
     * For `number` we assume pixels e.g. 5 => `5px`
     * For `string` you should provide the unit e.g. `5px`
     */
    export type BoxUnit = number | string;
    function mapBoxUnitToString(value: BoxUnit | null): string | null {
        if (typeof value === 'number') {
            return value.toString() + `px`;
        }
        else {
            return value;
        }
    }

    /**
     * A box function is something that can take:
     * - all
     * - topAndBottom + leftAndRight
     * - top + right + bottom + left
     */
    export interface BoxFunction<TUnit, TResult> {
        (all: TUnit): TResult;
        (topAndBottom: TUnit, leftAndRight: TUnit): TResult;
        (top: TUnit, right: TUnit, bottom: TUnit, left: TUnit): TResult;
    }

    /**
     * For use in simple functions
     */
    type Box<TUnit> = {
        top?: TUnit,
        right?: TUnit,
        bottom?: TUnit,
        left?: TUnit
    }

    /**
     * Takes a function that expects Box to be passed into it
     * and creates a BoxFunction
     */
    function createBoxFunction<TUnit, TBoxUnit, TResult>(mapFromUnit: (unit:TUnit) => TBoxUnit, mapFromBox: (box: Box<TBoxUnit>) => TResult): BoxFunction<TUnit, TResult> {
        const result: BoxFunction<TUnit, TResult> = (a: TUnit, b?: TUnit, c?: TUnit, d?: TUnit) => {
            if (b === undefined && c === undefined && d === undefined) {
                b = c = d = a;
            }
            else if (c === undefined && d === undefined) {
                c = a;
                d = b;
            }

            let box:Box<TBoxUnit> = {
                top: mapFromUnit(a),
                right: mapFromUnit(<any>b),
                bottom: mapFromUnit(<any>c),
                left: mapFromUnit(<any>d)
            };

            return mapFromBox(box);
        }
        return result;
    }

    export const position = createBoxFunction(mapBoxUnitToString, (box:Box<string>) => {
        return {
            top: box.top,
            right: box.right,
            bottom: box.bottom,
            left: box.left
        };
    });

    export const padding = createBoxFunction(mapBoxUnitToString, (box:Box<string>) => {
        return {
            paddingTop: box.top,
            paddingRight: box.right,
            paddingBottom: box.bottom,
            paddingLeft: box.left
        };
    });

    export const margin = createBoxFunction(mapBoxUnitToString, (box:Box<string>) => {
        return {
            marginTop: box.top,
            marginRight: box.right,
            marginBottom: box.bottom,
            marginLeft: box.left
        };
    });

    export const borderColor = createBoxFunction((s:string | null) => s, (box:Box<string>) => {
        return {
            borderTopColor: box.top,
            borderRightColor: box.right,
            borderBottomColor: box.bottom,
            borderLeftColor: box.left
        };
    });

    type BorderStyle =
        'none'
        | 'hidden'
        | 'dotted'
        | 'dashed'
        | 'solid'
        | 'double'
        | 'groove'
        | 'ridge'
        | 'inset'
        | 'outset'
        | 'initial'
        | 'inherit'

    export const borderStyle = createBoxFunction((style:BorderStyle | null) => style, (box:Box<BorderStyle>) => {
        return {
            borderTopStyle: box.top,
            borderRightStyle: box.right,
            borderBottomStyle: box.bottom,
            borderLeftStyle: box.left
        };
    });

    export const borderWidth = createBoxFunction(mapBoxUnitToString, (box:Box<string>) => {
        return {
            borderTopWidth: box.top,
            borderRightWidth: box.right,
            borderBottomWidth: box.bottom,
            borderLeftWidth: box.left
        };
    });
}
