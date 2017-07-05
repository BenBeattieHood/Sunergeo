import * as React from 'react';
import {connect} from 'react-redux';
import * as Styles from '../styles/core';

interface Props {
}

export class App extends React.Component<Props, {}> {
    render() {
        return (
            <div style={Styles.Palette.Controls.Base}>
                {this.props.children}
            </div>
        );
    }
}
