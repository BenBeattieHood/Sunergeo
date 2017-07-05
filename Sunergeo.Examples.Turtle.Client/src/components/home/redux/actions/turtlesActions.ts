import * as Redux from 'redux';
import { Turtle } from '../../../../services/serverDataTypes';

export type Action =
    ServerErroredAction
    | TurtleAddedAction
    | TurtleUpdatedAction
    ;

export const ServerErrored = 'ServerErrored'
export interface ServerErroredAction {
    type: 'ServerErrored'
}

export const TurtleAdded = 'TurtleAdded';
export interface TurtleAddedAction {
    type: 'TurtleAdded',
    turtle: Turtle,
}

export const TurtleUpdated = 'TurtleUpdated';
export interface TurtleUpdatedAction {
    type: 'TurtleUpdated',
    turtle: Turtle,
}

export interface Actions extends Redux.ActionCreatorsMapObject {
    serverErrored: () => ServerErroredAction,
    addTurtle: (turtle:Turtle) => TurtleAddedAction
    updateTurtle: (turtle:Turtle) => TurtleUpdatedAction,
}

export const actions:Actions = {
    serverErrored: () => ({
        type: 'ServerErrored'
    }),

    addTurtle: turtle => ({
        type: 'TurtleAdded',
        turtle
    }),

    updateTurtle: turtle => ({
        type: 'TurtleUpdated',
        turtle
    })
}
