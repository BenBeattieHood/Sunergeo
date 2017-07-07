import * as AlertMessageActions from '../actions/alertMessageActions';
import * as _ from 'lodash';

import { AlertMessage } from '../../../common/uiDataStructures';

export type State = AlertMessage[];
export const initialState:State = [];

export function reducer(state = initialState, action:AlertMessageActions.Action):State {
    switch (action.type) {
        case AlertMessageActions.AddAlertMessage:
            return [
                ...state,
                _.clone(action.alertMessage)
            ];

        case AlertMessageActions.AddAlertMessages:
            return [
                ...state,
                ..._.clone(action.alertMessages)
            ];
            
        case AlertMessageActions.RemoveAlertMessage:
            return _.filter(state, alertMessage => alertMessage !== action.alertMessage);

        case AlertMessageActions.ClearAlertMessages:
            return [];

        default:
            const x:never = action;
    }
    return state;
}