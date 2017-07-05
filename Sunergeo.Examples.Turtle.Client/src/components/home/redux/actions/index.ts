import * as ReduxUtils from '../../../../utils/reduxUtils';
import * as AlertMessageActions from '../../../common/redux/actions/alertMessageActions';
import * as TurtlesActions from './turtlesActions';

export type Actions =
    TurtlesActions.Actions
    & AlertMessageActions.Actions
    ;

export const actions:Actions =
    ReduxUtils.combine(
        TurtlesActions.actions,
        AlertMessageActions.actions,
    )
