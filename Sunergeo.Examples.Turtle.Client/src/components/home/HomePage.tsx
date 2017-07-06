import * as React from 'react';
import * as _ from 'lodash';

// Redux
import * as ReduxUtils from '../../utils/reduxUtils';
import { Actions as ReduxActions, actions as reduxActions } from './redux/actions/index';
import { State as ReduxState, initialState as reduxInitialState } from './redux/reducers/index';
import configureStore from './redux/store/configureStore';

// Server interop
import { Turtle } from '../../services/serverDataTypes';
import { TurtleApi } from '../../services/TurtleApi';

// UI controls
import * as Bootstrap from 'react-bootstrap';
import * as Styles from '../../styles/core';
import { Loader } from '../common/Loader';
import { FlexBox } from '../common/FlexBox';
import { MessageBox } from '../messageBoxModal/index'
import { TextField } from '../common/TextField';

const turtleImage = require('./turtle.png');

namespace BaseStyles {
    export const root = Styles.compose(
    )
}

class JournalsPage extends ReduxUtils.ReduxContainer<{}, ReduxState, ReduxActions> {

    private turtleApi:TurtleApi = new TurtleApi(
        ""
        );

    componentWillMount() {
        this.props.actions.updateIsLoading(true);
        this.turtleApi.getAll()
        .then(turtles => {
            _.forEach(turtles, this.props.actions.addTurtle);
            this.props.actions.updateIsLoading(false);
        })
        .catch((error:Error) => {
            this.props.actions.updateIsLoading(false);
            this.props.actions.addAlertMessage({
                type: 'Error',
                message: error.message
            });
            this.props.actions.serverErrored()
        })
    }

    renderAlertMessage() {
        return (
            <MessageBox
                title={this.props.alertMessages.length > 0 ? this.props.alertMessages[0].type : ""}
                message={this.props.alertMessages.length > 0 ? this.props.alertMessages[0].message : ""}
                show={this.props.alertMessages.length > 0}
                onAccept={() => this.props.actions.removeAlertMessage(this.props.alertMessages[0])}
                />
        )
    }

    canvasElement:HTMLCanvasElement | null = null;

    render() {
        if (this.props.serverEntities.isLoading) {
            return (
                <Loader
                    visible={true}
                    />
            );
        }

        return (
            <div style={BaseStyles.root}>
                <div className="container-fluid">
                    <div
                        className="container"
                        >
                        {this.renderAlertMessage()}
                        <div>
                            <canvas
                                ref={el => this.canvasElement = el}
                                width="800px"
                                height="800px"
                                />
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    componentDidMount() {
        this.rerenderTurtles();
    }

    componentDidUpdate() {
        this.rerenderTurtles();
    }

    rerenderTurtles() {
        if (this.canvasElement !== null) {
            var context = this.canvasElement.getContext('2d'); 
                _.forEach(this.props.serverEntities.turtles, turtle => {
                    var imageObj = new Image();

                    imageObj.onload = function() {                        
                        if (context !== null) {
                            context.drawImage(imageObj, turtle.positions[turtle.positions.length -1].x, turtle.positions[turtle.positions.length -1].y);
                        }
                    };
                    imageObj.src = turtleImage;
                });
        }
    }
}

export default ReduxUtils.reduxContainer<ReduxState, ReduxActions, {}>({
    reduxInitialState,
    configureStore,
    componentClass: JournalsPage,
    reduxActions
})
