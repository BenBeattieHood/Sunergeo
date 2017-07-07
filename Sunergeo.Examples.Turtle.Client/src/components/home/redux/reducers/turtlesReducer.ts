import * as TurtlesActions from '../actions/turtlesActions';
import { Turtle } from '../../../../services/serverDataTypes';
import * as _ from 'lodash';

export type State = {
    turtles: Turtle[],
    isLoading: boolean
};
export const initialState:State = {
    turtles: [],
    isLoading: true
}

export function reducer(state = initialState, action:TurtlesActions.Action):State {
    switch (action.type) {
        case TurtlesActions.ServerErrored:
            return {
                ...state,
                isLoading: false
            };

        case TurtlesActions.IsLoadingUpdated:
            return {
                ...state,
                isLoading: action.isLoading
            }

        case TurtlesActions.TurtleAdded:
            return {
                ...state,
                turtles: [
                    _.clone(action.turtle),
                    ...state.turtles
                ]
            };

        case TurtlesActions.TurtleUpdated:
            return {
                ...state,
                turtles: _.map(state.turtles, turtle =>
                    turtle.turtleId === action.turtle.turtleId
                    ? _.clone(action.turtle)
                    : turtle
                    )
            };

        default:
            const x:never = action;
    }
    return state;
}
