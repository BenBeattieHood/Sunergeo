import * as Redux from 'redux';
import { Turtle } from '../../../../services/serverDataTypes';

export type Action =
    ServerErroredAction
    | IsLoadingUpdatedAction
    | TurtleAddedAction
    | TurtleUpdatedAction
    ;

export const ServerErrored = 'ServerErrored'
export interface ServerErroredAction {
    type: 'ServerErrored'
}

export const IsLoadingUpdated = 'IsLoadingUpdated'
export interface IsLoadingUpdatedAction {
    type: 'IsLoadingUpdated',
    isLoading: boolean
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

    updateIsLoading: isLoading => ({
        type: 'IsLoadingUpdated',
        isLoading
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
