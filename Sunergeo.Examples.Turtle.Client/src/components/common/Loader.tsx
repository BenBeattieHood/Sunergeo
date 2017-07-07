import * as React from 'react';
import * as Styles from '../../styles/core';

interface Props {
    visible: boolean,
    style?: React.CSSProperties
}

namespace BaseStyles {
    export const root:React.CSSProperties = {
        textAlign: 'center'
    }
}

export const Loader = (props:Props) =>
    <div style={BaseStyles.root}>
        <div style={Styles.compose(props.style, { visibility: props.visible ? 'visible' : 'hidden' })}>
            <div className="pageup-loader" />
        </div>
    </div>