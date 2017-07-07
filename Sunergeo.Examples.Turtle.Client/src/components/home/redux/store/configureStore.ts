import {createStore, applyMiddleware} from 'redux';
import { rootReducer, State } from '../reducers';
import thunk from 'redux-thunk';

export default function configureStore(initialState?:State) {
    return createStore(
        rootReducer,
        initialState,
        applyMiddleware(thunk)
    );
}