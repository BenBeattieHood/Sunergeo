import * as React from 'react';
import { ParentNode } from './uiDataStructures';
import * as _ from 'lodash';
import { FlexBox } from './FlexBox';

export const Concat = (props: ParentNode & {
    header?: JSX.Element,
    separator:string,
    style?:React.CSSProperties
}) => {
    const childRenders = React.Children.map(props.children, (child, index) => (():React.ReactNode[] =>
        [
            child,
            index === (childRenders.length - 1) ? null : <span key={`concat_sep_${index}`} style={{display:'inline-block', paddingRight: 4}}>,</span>
        ]
    ));

    return (
        <span
            style={{
                display: 'block'
            }}
            >
            {props.header &&
                <span
                    style={{display:'inline-block', paddingRight: 5}}
                    >
                    {props.header}
                </span>
            }
            {_.filter(_.flatten(_.map(childRenders, render => render())), x => x)}
        </span>
    );
}
