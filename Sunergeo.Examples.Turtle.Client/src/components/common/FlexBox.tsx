import * as React from 'react'
import * as _ from 'lodash'

import * as Styles from '../../styles/core';
import { browserSpec } from '../../utils/browser';
require('../../polyfills/cssSupports/index');

interface Props {
    direction: 'Horizontal' | 'Vertical',
    inline: boolean
    style?: React.CSSProperties
}

const DefaultFlexBox = (props:Readonly<Props>) => {
    const baseStyle =
        props.direction === 'Horizontal' && props.inline === true
        ? Styles.FlexLayout.InlineContainerHorizontal
        : props.direction === 'Horizontal' && props.inline === false
        ? Styles.FlexLayout.BlockContainerHorizontal
        : props.direction === 'Vertical' && props.inline === true
        ? Styles.FlexLayout.InlineContainerVertical
        : props.direction === 'Vertical' && props.inline === false
        ? Styles.FlexLayout.BlockContainerVertical
        : undefined;

    if (baseStyle === undefined) {
        throw new Error("Invalid props config");
    }

    return (
        <div style={Styles.compose(
            baseStyle,
            props.style
        )}>
            {(props as any).children}
        </div>
    )
}

const PolyfillFlexBox = (props:Readonly<Props>) => {
    const children = _.filter<React.ReactChild>((props as any).children, child => child);
    const totalFlex = _.reduce(
        children,
        (state, next) => {
            const childProps = (next as any).props;
            if (childProps) {
                const style = childProps.style;
                const flexGrow:number | undefined = style && style.flexGrow;
                return state + (flexGrow || 0);
            }
            return state;
        },
        0
    );
    let childrenWithLayout = React.Children.map(children, (child, index) => {
        const childProps = (child as any).props;
        const style:(React.CSSProperties | undefined) = childProps && childProps.style;

        const flexAlignItems = style && style.alignItems;
        if (style && flexAlignItems) { delete style.alignItems; }
        const flexJustifyContent = style && style.justifyContent;
        if (style && flexJustifyContent) { delete style.justifyContent; }

        const cellStyle:React.CSSProperties = {
            textAlign: (():('left' | 'right' | 'center' | undefined) => {
                switch (flexJustifyContent) {
                    case 'flex-start': return 'left';
                    case 'flex-end': return 'right';
                    case 'center': return 'center';
                    case 'space-between':
                    case 'space-around':
                    case 'space-evenly':
                        return 'center';
                    default:
                        return undefined;
                }
            })(),
            verticalAlign: (():('top' | 'bottom' | 'middle' | undefined) => {
                switch (flexJustifyContent) {
                    case 'flex-start': return 'top';
                    case 'flex-end': return 'bottom';
                    case 'center': return 'middle';
                    case 'space-between':
                    case 'space-around':
                    case 'space-evenly':
                        return 'middle';
                    default:
                        return undefined;
                }
            })()
        };

        switch (props.direction) {
            case 'Vertical':
                if (style && style.cachedFlexCellStyle === undefined) {
                    const flexGrowHeight = style.flexGrow;
                    if (flexGrowHeight) {
                        delete style.flexGrow;
                        delete style.flexShrink;
                        if (!style.height && typeof flexGrowHeight === "number") {
                            style.height = `${(flexGrowHeight / totalFlex) * 100}%`;
                        }
                    }

                    const height = style && style.height;
                    if (height) { style.height = '100%'; }
                    const minHeight = style && style.minHeight;
                    if (minHeight) { style.minHeight = '100%'; }
                    const maxHeight = style && style.maxHeight;
                    if (maxHeight) { style.maxHeight = '100%'; }
                    cellStyle.height = height;
                    cellStyle.minHeight = minHeight;
                    cellStyle.maxHeight = maxHeight;
                    style.cachedFlexCellStyle = cellStyle;
                }

                return (
                    <tr key={index}>
                        <td style={style && style.cachedFlexCellStyle}>
                            {child}
                        </td>
                    </tr>
                );
            case 'Horizontal':
                if (style && style.cachedFlexCellStyle === undefined) {
                    const flexGrowWidth = style.flexGrow;
                    if (flexGrowWidth) {
                        delete style.flexGrow;
                        delete style.flexShrink;
                        if (!style.width && typeof flexGrowWidth === "number") {
                            style.width = `${(flexGrowWidth / totalFlex) * 100}%`;
                        }
                    }

                    const width = style && style.width;
                    if (width) { style.width = '100%'; }
                    const minWidth = style && style.minWidth;
                    if (minWidth) { style.minWidth = '100%'; }
                    const maxWidth = style && style.maxWidth;
                    if (maxWidth) { style.maxWidth = '100%'; }
                    cellStyle.width = width;
                    cellStyle.minWidth = minWidth;
                    cellStyle.maxWidth = maxWidth;
                    style.cachedFlexCellStyle = cellStyle;
                }

                return (
                    <td key={index} style={style && style.cachedFlexCellStyle}>
                        {child}
                    </td>
                );
            default:
                const x:never = props.direction;
                return x;
        }
    }) as any;

    if (props.direction === 'Horizontal') {
        childrenWithLayout = <tr>{childrenWithLayout}</tr>
    }

    return (
        <table
            style={Styles.compose(
                Styles.Box.borderWidth(0),
                {
                    borderCollapse: 'collapse',
                    width: '100%'
                },
                props.style
            )}
            >
            <tbody>
                {childrenWithLayout}
            </tbody>
        </table>
    );
}

const cssSupportsFlex = CSS.supports('display', 'flex')
const browserIsUnsupportedVersionOfIE = (browserSpec.name === "IE" && browserSpec.version && browserSpec.version <= 10)
const shouldUseFlexboxPolyfill = !cssSupportsFlex || browserIsUnsupportedVersionOfIE

export const FlexBox = (props:Props) =>

    shouldUseFlexboxPolyfill ?
    <PolyfillFlexBox {...props} />
    : <DefaultFlexBox {...props} />
