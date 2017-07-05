import { combineReducers } from 'redux';
import * as TurtlesReducer from './turtlesReducer';
import * as AlertMessageReducer from '../../../common/redux/reducers/alertMessageReducer';

// NB redux's combineReducers is a bit of a JS/dynamic 'hack': its keys need to be the same as your Redux state's keys
export const rootReducer = combineReducers({
    turtles: TurtlesReducer.reducer,
    alertMessages: AlertMessageReducer.reducer,
});

export interface State {
    turtles: TurtlesReducer.State,
    alertMessages: AlertMessageReducer.State,
}

export const initialState:State = {
    turtles: TurtlesReducer.initialState,
    alertMessages: AlertMessageReducer.initialState,
}
