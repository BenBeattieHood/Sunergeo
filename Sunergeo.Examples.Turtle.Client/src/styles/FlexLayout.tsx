import * as React from 'react';

export namespace FlexLayout {
    export const Flex = (flexGrow: number = 1, flexShrink?: number):React.CSSProperties => ({
        flexGrow,
        flexShrink: (flexShrink == null ? flexGrow : flexShrink)
    })
    export const BlockContainerHorizontal:React.CSSProperties = {
        display: 'flex',
        flexDirection: 'row'
    }
    export const BlockContainerVertical:React.CSSProperties = {
        display: 'flex',
        flexDirection: 'column'
    }
    export const InlineContainerHorizontal:React.CSSProperties = {
        display: 'inline-flex',
        flexDirection: 'row'
    }
    export const InlineContainerVertical:React.CSSProperties = {
        display: 'inline-flex',
        flexDirection: 'column'
    }
    export enum Wrapping {
        Wrap,
        NoWrap
    }
    export const Wrap = (wrapping:Wrapping):React.CSSProperties => {
        switch (wrapping) {
            case Wrapping.Wrap: return { flexWrap: 'wrap' };
            case Wrapping.NoWrap: return { flexWrap: 'nowrap' };

            default: const x:never = wrapping; // this is a TS exhaustiveness check
        }
        return {};  // TODO: remove this, but there's a bug in TS 2.2.1s exhaustiveness vs null-return checks
    }
    export enum Alignment {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        CenterCenter,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
    export const Align = (alignment:Alignment):React.CSSProperties => {
        switch (alignment) {
            case Alignment.TopLeft: return { alignItems: 'flex-start', justifyContent: 'flex-start' };
            case Alignment.TopCenter: return { alignItems: 'flex-start', justifyContent: 'center' };
            case Alignment.TopRight: return { alignItems: 'flex-start', justifyContent: 'flex-end' };

            case Alignment.CenterLeft: return { alignItems: 'center', justifyContent: 'flex-start' };
            case Alignment.CenterCenter: return { alignItems: 'center', justifyContent: 'center' };
            case Alignment.CenterRight: return { alignItems: 'center', justifyContent: 'flex-end' };

            case Alignment.BottomLeft: return { alignItems: 'flex-end', justifyContent: 'flex-start' };
            case Alignment.BottomCenter: return { alignItems: 'flex-end', justifyContent: 'center' };
            case Alignment.BottomRight: return { alignItems: 'flex-end', justifyContent: 'flex-end' };

            default: const x:never = alignment; // this is a TS exhaustiveness check
        }
        return {};  // TODO: remove this, but there's a bug in TS 2.2.1s exhaustiveness vs null-return checks
    }
}
