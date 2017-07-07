import * as React from 'react';
import * as Redux from 'redux';
import { Provider, connect } from 'react-redux';
import {bindActionCreators, Dispatch} from 'redux';
import * as _ from 'lodash';

export interface ReduxProps<T> {
    actions: T
}

export function reduxContainer<TReduxState, TReduxActions extends Redux.ActionCreatorsMapObject, TProps>(args:{
    reduxInitialState: TReduxState,
    configureStore: (initialState?:TReduxState) => Redux.Store<any>,
    componentClass: React.ComponentClass<TProps & TReduxState & ReduxProps<TReduxActions>> | React.StatelessComponent<TProps & TReduxState & ReduxProps<TReduxActions>>,
    reduxActions: TReduxActions
}):(props: TProps & { reduxStoreOverrides?: Partial<TReduxState> }) => JSX.Element {

    let mapReduxStateToProps = (state:any, ownProps:any):TProps => ({
        ...state,
        ...ownProps
    });
    let attachRedux = (dispatch:Dispatch<any>) => ({
        actions: bindActionCreators(args.reduxActions, dispatch)
    })

    let ReduxedComponentClass = connect(mapReduxStateToProps, attachRedux)(args.componentClass);

    return (props: TProps & { reduxStoreOverrides?: Partial<TReduxState> }) => {
        const initialStoreState:TReduxState = combine(
            args.reduxInitialState,
            props.reduxStoreOverrides
        );
        const store = args.configureStore(initialStoreState);
        const componentProps = _.omit(props, ['reduxStoreOverrides']);
        return (
            <Provider store={store}>
                <ReduxedComponentClass {...componentProps} />
            </Provider>
        );
    };
}

export abstract class ReduxContainer<Props, ReduxState, ReduxActions> extends React.Component<Props & ReduxState & ReduxProps<ReduxActions>, {}> {}

export function combine<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(v1:T1, v2?:T2, v3?:T3, v4?:T4, v5?:T5, v6?:T6, v7?:T7, v8?:T8, v9?:T9, v10?:T10):T1 & T2 & T3 & T4 & T5 & T6 & T7 & T8 & T9 & T10 {
    return _.extend({}, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10) as any;
}
