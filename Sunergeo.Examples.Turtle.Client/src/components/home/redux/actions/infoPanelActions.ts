import * as Redux from 'redux';
import * as InfoPanelReducer from '../reducers/infoPanelReducer';

export type Action = 
    SetInfoPanelAction

export const SetInfoPanel = 'SetInfoPanel';
export interface SetInfoPanelAction {
    type: 'SetInfoPanel',
    infoPanel: InfoPanelReducer.State
}

export interface Actions extends Redux.ActionCreatorsMapObject {
    showInfoPanel: () => void,
    closeInfoPanel: () => void
}
export const actions:Actions = {
    showInfoPanel: () => ({
        type: 'SetInfoPanel',
        infoPanel: InfoPanelReducer.ShowInfoPanelLiteral
    }),
    closeInfoPanel: () => ({
        type: 'SetInfoPanel',
        infoPanel: null
    })
}