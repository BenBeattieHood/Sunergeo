import * as React from 'react';
import * as _ from 'lodash';

import * as Styles from '../../styles/core';
require('../../polyfills/weakMap/index')
require('../../polyfills/mutationObserver/index')
require('../../polyfills/animationFrame/index').shim()

export enum WaypointPosition {
    Before,
    Visible,
    After
}


interface Props {
    onChange: (position: WaypointPosition) => void,
    direction: "Horizontal" | "Vertical"
}

interface State {
}

interface PositionData {
    start: number,
    end: number
}
type ScrollContainer = { type: 'Element', value: HTMLElement } | { type: 'Document', value: HTMLDocument } | null;

export class Waypoint extends React.Component<Props, State> {
    mutationObserver:MutationObserver = new ((window as any)['PolyfilledMutationObserver'])(() => this.refresh());
    mutationObserverConnected:boolean = false;

    getScrollParent(element:HTMLElement, includeOverflowHidden:boolean):ScrollContainer {
        let style = getComputedStyle(element);
        let excludeStaticParent = style.position === "absolute";
        let overflowRegex = includeOverflowHidden ? /(auto|scroll|hidden)/ : /(auto|scroll)/;

        if (style.position === "fixed") {
            return { type: 'Document', value: document };
        }
        for (let parent:HTMLElement | null = element; (parent = parent.parentElement);) {
            style = getComputedStyle(parent);
            if (excludeStaticParent && style.position === "static") {
                continue;
            }
            if (overflowRegex.test((style.overflow || '') + (style.overflowY || '') + (style.overflowX || ''))) {
                return { type: 'Element', value: parent };
            }
        }

        return { type: 'Document', value: document };
    }

    componentDidMount() {
        if (this.element) {
            this.scrollContainer = this.getScrollParent(this.element, true);
            if (this.scrollContainer) {
                this.mutationObserver.observe(
                    this.scrollContainer.value,
                    {
                        childList: true,
                        subtree: true,
                        characterData: true
                    }
                );
                this.mutationObserverConnected = true;
                this.refresh();
                (this.scrollContainer.type === 'Document' ? window : this.scrollContainer.value).addEventListener('scroll', this.refresh);
            }
        }
    }

    componentWillUnmount() {
        if (this.currentAnimationFrameId !== null) {
            let currentAnimationFrameId = this.currentAnimationFrameId;
            this.currentAnimationFrameId = null;
            window.cancelAnimationFrame(currentAnimationFrameId);
        }
        if (this.mutationObserverConnected) {
            this.mutationObserver.disconnect();
        }
        if (this.scrollContainer) {
            (this.scrollContainer.type === 'Document' ? window : this.scrollContainer.value).removeEventListener('scroll', this.refresh);
        }
    }

    element:HTMLSpanElement | null = null;
    scrollContainer:ScrollContainer = null;
    currentAnimationFrameId:number | null = null;

    render() {
        return (
            <span
                ref={el => this.element = el}
                style={{height:0, width:0, lineHeight:0, fontSize:0, overflow:'hidden'}}
                />
        );
    }

    getWindowBoundingRect = ():ClientRect => ({
        bottom: window.innerHeight,
        height: window.innerHeight,
        left: 0,
        right: window.innerWidth,
        top: 0,
        width: window.innerWidth
    })

    getPositionData = (clientRect:ClientRect):PositionData => {
        switch (this.props.direction) {
            case "Horizontal":
                return {
                    start: clientRect.left,
                    end: clientRect.right
                }
            case "Vertical":
                return {
                    start: clientRect.top,
                    end: clientRect.bottom
                }
            default:
                const x:never = this.props.direction;
                return x;
        }
    }

    lastEmittedPosition:WaypointPosition | undefined = undefined;

    refresh = () => {
        if (this.currentAnimationFrameId === undefined) {
            this.currentAnimationFrameId = window.requestAnimationFrame(() => {
                if (this.element && this.scrollContainer) {
                    let scrollContainerClientRect =
                        this.scrollContainer.type === 'Document'
                        ? this.getWindowBoundingRect()
                        : this.scrollContainer.value.getBoundingClientRect()
                        ;
                    let elementClientRect = this.element.getBoundingClientRect();

                    let scrollContainerBounds = this.getPositionData(scrollContainerClientRect);
                    let elementBounds = this.getPositionData(elementClientRect);

                    let position =
                        (scrollContainerBounds.start < elementBounds.end && scrollContainerBounds.end > elementBounds.start)
                        ? WaypointPosition.Visible
                        : scrollContainerBounds.start > elementBounds.end
                        ? WaypointPosition.Before
                        : scrollContainerBounds.end < elementBounds.start
                        ? WaypointPosition.After
                        : undefined
                        ;

                    if (position !== undefined && position !== this.lastEmittedPosition) {
                        this.lastEmittedPosition = position;
                        this.props.onChange(position);
                    }
                    this.currentAnimationFrameId = null;
                }
            });
        }
    }
}
