import * as InfoPanelActions from '../actions/infoPanelActions';

export const ShowInfoPanelLiteral = 'show';
export type ShowInfoPanelLiteralType = 'show';
export type State = ShowInfoPanelLiteralType | null;
export const initialState:State = null;

export function reducer(state = initialState, action:InfoPanelActions.Action) {
    switch (action.type) {
        case InfoPanelActions.SetInfoPanel:
            return action.infoPanel;
    }
    return state;
}
