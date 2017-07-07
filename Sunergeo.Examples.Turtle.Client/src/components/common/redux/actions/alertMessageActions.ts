import * as Redux from 'redux';
import { AlertMessage } from '../../../common/uiDataStructures';

export type Action =
    AddAlertMessageAction
    | AddAlertMessagesAction
    | RemoveAlertMessageAction
    | ClearAlertMessagesAction
    ;

export const AddAlertMessage = 'AddAlertMessage';
export interface AddAlertMessageAction {
    type: 'AddAlertMessage',
    alertMessage: AlertMessage
}

export const AddAlertMessages = 'AddAlertMessages';
export interface AddAlertMessagesAction {
    type: 'AddAlertMessages',
    alertMessages: AlertMessage[]
}

export const RemoveAlertMessage = 'RemoveAlertMessage';
export interface RemoveAlertMessageAction {
    type: 'RemoveAlertMessage',
    alertMessage: AlertMessage
}

export const ClearAlertMessages = 'ClearAlertMessages';
export interface ClearAlertMessagesAction {
    type: 'ClearAlertMessages'
}

export interface Actions extends Redux.ActionCreatorsMapObject {
    addAlertMessage: (alertMessage:AlertMessage) => AddAlertMessageAction
    addAlertMessages: (alertMessages:AlertMessage[]) => AddAlertMessagesAction
    removeAlertMessage: (alertMessage:AlertMessage) => RemoveAlertMessageAction
    clearAlertMessages: () => ClearAlertMessagesAction
}

export const actions:Actions = {
    addAlertMessage: alertMessage => ({
        type: 'AddAlertMessage',
        alertMessage
    }),
    addAlertMessages: alertMessages => ({
        type: 'AddAlertMessages',
        alertMessages
    }),
    removeAlertMessage: alertMessage => ({
        type: 'RemoveAlertMessage',
        alertMessage
    }),
    clearAlertMessages: () => ({
        type: 'ClearAlertMessages'
    })
}