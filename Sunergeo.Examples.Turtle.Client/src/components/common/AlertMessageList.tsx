import * as React from 'react';
import * as _ from 'lodash';
import * as Bootstrap from 'react-bootstrap';

import * as UiDataTypes from './uiDataStructures';

interface Props {
    items: UiDataTypes.AlertMessage[],
    onCloseItem: (item:UiDataTypes.AlertMessage) => void
}

const convertAlertTypeToBsStyle = (alert:UiDataTypes.AlertMessage):string => {
    switch (alert.type) {
        case 'Success':
            return 'success';

        case 'Warning':
            return 'warning';

        case 'Info':
            return 'info';

        case 'Error':
            return 'danger';

        default:
            const x:never = alert;
            return '';
    }
}

export const AlertMessageList = (props:Props) => {
    return (
        <div>
            {_.map(props.items, (item, index) => 
                <Bootstrap.Alert
                    key={index}
                    bsStyle={convertAlertTypeToBsStyle(item)}
                    onDismiss={() => props.onCloseItem(item)}
                    >
                    <div dangerouslySetInnerHTML={{__html:item.message}} />
                </Bootstrap.Alert>
            )}
        </div>
    )
}