import * as React from 'react';
import * as _ from 'lodash';

// Redux
import * as ReduxUtils from '../../utils/reduxUtils';
import { Actions as ReduxActions, actions as reduxActions } from './redux/actions/index';
import { State as ReduxState, initialState as reduxInitialState } from './redux/reducers/index';
import configureStore from './redux/store/configureStore';

// Server interop
import { Turtle, Direction } from '../../services/serverDataTypes';
import { TurtleApi } from '../../services/TurtleApi';

// UI controls
import * as Bootstrap from 'react-bootstrap';
import * as Styles from '../../styles/core';
import { Loader } from '../common/Loader';
import { FlexBox } from '../common/FlexBox';
import { MessageBox } from '../messageBoxModal/index'
import { TextField } from '../common/TextField';

const turtleImageNorth = require('./turtle-north.png');
const turtleImageEast = require('./turtle-east.png');
const turtleImageSouth = require('./turtle-south.png');
const turtleImageWest = require('./turtle-west.png');

const scaleFactor = 3;
const canvasWidth = 260 * scaleFactor;
const canvasHeight = 260 * scaleFactor;
const turtleMovementUnit = 10 * scaleFactor;
const canvasHalfWidth = canvasWidth / 2;
const canvasHalfHeight = canvasHeight / 2;
const turtleImageWidth = 42 * scaleFactor;
const turtleImageHeight = 60 * scaleFactor;
const turtleHalfImageWidth = turtleImageWidth / 2;
const turtleHalfImageHeight = turtleImageHeight / 2;

let canvasBorderStyle = {
    border: "1px solid #000000"
}

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

    createTurtle() {
        //TODO: Need to call TurtleApi for this

        let turtle: Turtle = {
            turtleId: 1,
            positions: [
                { x: 0, y: 0 }
            ],
            direction: Direction.East,
            isVisible: true
        }

        this.props.actions.addTurtle(turtle);
    }

    turnLeft() {
        //TODO: Need to call TurtleApi for this
        let turtle = this.props.serverEntities.turtles[0];
        let updatedTurtle: Turtle = {
            ...turtle
        }

        switch (updatedTurtle.direction) {
            case Direction.North:
                updatedTurtle.direction = Direction.West;
                break;
            case Direction.East:
                updatedTurtle.direction = Direction.North;
                break;
            case Direction.South:
                updatedTurtle.direction = Direction.East;
                break;
            case Direction.West:
                updatedTurtle.direction = Direction.South;
                break;
        }

        this.props.actions.updateTurtle(updatedTurtle);
    }

    turnRight() {
        //TODO: Need to call TurtleApi for this

        let turtle = this.props.serverEntities.turtles[0];
        let updatedTurtle: Turtle = {
            ...turtle
        }

        switch (updatedTurtle.direction) {
            case Direction.North:
                updatedTurtle.direction = Direction.East;
                break;
            case Direction.East:
                updatedTurtle.direction = Direction.South;
                break;
            case Direction.South:
                updatedTurtle.direction = Direction.West;
                break;
            case Direction.West:
                updatedTurtle.direction = Direction.North;
                break;
        }

        this.props.actions.updateTurtle(updatedTurtle);
    }

    moveForwards() {
        //TODO: Need to call TurtleApi for this

        let turtle = this.props.serverEntities.turtles[0];
        let updatedTurtle: Turtle = {
            ...turtle
        }

        switch (updatedTurtle.direction) {
            case Direction.North:
                updatedTurtle.positions.push({ x: turtle.positions[turtle.positions.length - 1].x, y: turtle.positions[turtle.positions.length - 1].y - turtleMovementUnit });
                break;
            case Direction.East:
                updatedTurtle.positions.push({ x: turtle.positions[turtle.positions.length - 1].x + turtleMovementUnit, y: turtle.positions[turtle.positions.length - 1].y });
                break;
            case Direction.South:
                updatedTurtle.positions.push({ x: turtle.positions[turtle.positions.length - 1].x, y: turtle.positions[turtle.positions.length - 1].y + turtleMovementUnit });
                break;
            case Direction.West:
                updatedTurtle.positions.push({ x: turtle.positions[turtle.positions.length - 1].x - turtleMovementUnit, y: turtle.positions[turtle.positions.length - 1].y });
                break;
        }

        this.props.actions.updateTurtle(updatedTurtle);
    }

    canvasElement: HTMLCanvasElement | null = null;

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
                    <div className="container">
                        {this.renderAlertMessage()}
                        <div>
                            <canvas
                                ref={el => this.canvasElement = el}
                                width={canvasWidth}
                                height={canvasHeight}
                                style={canvasBorderStyle}
                            />
                            <br />
                            <br />
                            <Bootstrap.ButtonToolbar>
                                <Bootstrap.ButtonGroup>
                                    <Bootstrap.Button onClick={() => this.createTurtle()}>Create</Bootstrap.Button>
                                    <Bootstrap.Button onClick={() => this.turnLeft()}>Turn left</Bootstrap.Button>
                                    <Bootstrap.Button onClick={() => this.turnRight()}>Turn right</Bootstrap.Button>
                                    <Bootstrap.Button onClick={() => this.moveForwards()}>Move forward</Bootstrap.Button>
                                </Bootstrap.ButtonGroup>
                            </Bootstrap.ButtonToolbar>
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
        try {            
            if (this.canvasElement !== null) {
                var context = this.canvasElement.getContext('2d');
                _.forEach(this.props.serverEntities.turtles, turtle => {
                    var imageObj = new Image();

                    switch (turtle.direction) {
                        case Direction.North:
                            imageObj.src = turtleImageNorth;
                            break;
                        case Direction.East:
                            imageObj.src = turtleImageEast;
                            break;
                        case Direction.West:
                            imageObj.src = turtleImageWest;
                            break;
                        case Direction.South:
                            imageObj.src = turtleImageSouth;
                            break;
                        default:
                            imageObj.src = turtleImageNorth;
                            break;
                    }

                    imageObj.onload = function () {
                        if (context !== null) {

                            // Clear the canvas
                            context.clearRect(0, 0, canvasWidth, canvasHeight);

                            // Draw turtle path
                            context.beginPath();
                            context.moveTo(turtle.positions[0].x + canvasHalfWidth, turtle.positions[0].y + canvasHalfHeight);
                            for (var i = 1; i < turtle.positions.length; i++) {
                                context.lineTo(turtle.positions[i].x + canvasHalfWidth, turtle.positions[i].y + canvasHalfHeight);
                            }
                            context.strokeStyle = "blue";
                            context.stroke();

                            // Draw turtle final position and orientation
                            switch (turtle.direction) {
                                case Direction.North:
                                case Direction.South:
                                    context.drawImage(imageObj, turtle.positions[turtle.positions.length - 1].x + canvasHalfWidth - turtleHalfImageWidth, turtle.positions[turtle.positions.length - 1].y + canvasHalfHeight - turtleHalfImageHeight, turtleImageWidth, turtleImageHeight);
                                    break;
                                case Direction.East:
                                case Direction.West:
                                    context.drawImage(imageObj, turtle.positions[turtle.positions.length - 1].x + canvasHalfWidth - turtleHalfImageHeight, turtle.positions[turtle.positions.length - 1].y + canvasHalfHeight - turtleHalfImageWidth, turtleImageHeight, turtleImageWidth);
                                    break;
                            }
                        }
                    };
                });
            }
        } catch (error) {
            this.props.actions.updateIsLoading(false);
                this.props.actions.addAlertMessage({
                    type: 'Error',
                    message: error.message
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
