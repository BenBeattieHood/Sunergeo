import * as React from 'react';
import * as _ from 'lodash';

// Redux
import * as ReduxUtils from '../../utils/reduxUtils';
import { Actions as ReduxActions, actions as reduxActions } from './redux/actions/index';
import { State as ReduxState, initialState as reduxInitialState } from './redux/reducers/index';
import configureStore from './redux/store/configureStore';

// Server interop
import { Turtle } from '../../services/serverDataTypes';
import { TurtleApi } from '../../services/TurtleApi';

// UI controls
import * as Bootstrap from 'react-bootstrap';
import * as Styles from '../../styles/core';
import { Loader } from '../common/Loader';
import { FlexBox } from '../common/FlexBox';
import { MessageBox } from '../messageBoxModal/index'
import { TextField } from '../common/TextField';

import { MonolithConfig } from '../../utils/environment'

namespace BaseStyles {
    export const root = Styles.compose(
    )
}

class JournalsPage extends ReduxUtils.ReduxContainer<{}, ReduxState, ReduxActions> {

    private turtleApi:TurtleApi = new TurtleApi(
        ""
        );

    loadingMoreJournals:boolean = false;
    loadMoreJournals = () => {
        if (!this.loadingMoreJournals && this.props.journals.serverHasMoreItems) {
            this.loadingMoreJournals = true;
            this.journalApi.search({
                term: this.props.searchCriteria.term,
                lastResultId: this.props.journals.entries.length ? _.last(this.props.journals.entries).id : undefined
            })
            .then(results => {
                this.loadingMoreJournals = false;
                this.props.actions.addJournalsFromServer(results.entries, results.more);
            })
            .catch((error:Error) => {
                this.loadingMoreJournals = false;
                this.props.actions.addAlertMessage({
                    type: 'Error',
                    message: error.message
                });
                this.props.actions.serverErrored()
            })
        }
    }

    componentWillMount() {
        this.turtleApi
        .getAll()
        .catch((error:Error) => {
            this.props.actions.addAlertMessage({
                type: 'Error',
                message: error.message
            });
        })
        .then(this.props.actions.addTagsFromServer)
    }

    renderAlertMessage() {
        return (
            <MessageBox
                title={this.props.alertMessages.length > 0 ? this.props.alertMessages[0].type : ""}
                message={this.props.alertMessages.length > 0 ? this.props.alertMessages[0].message : ""}
                show={this.props.alertMessages.length > 0}
                onAccept={() => this.props.actions.removeAlertMessage(this.props.alertMessages[0])}
                />
        )
    }

    renderInfoPanel() {
        const infoPanel = this.props.infoPanel;
        if (infoPanel === null) {
            return null;
        }
        else {
            const closeInfoPanel = () => this.props.actions.closeInfoPanel();
            return (
                <Bootstrap.Alert
                    bsStyle="info"
                    onDismiss={this.props.actions.closeInfoPanel}
                    >
                    <p style={{fontWeight:'bold'}}>{this.props.language.JOURNAL_MAILMATCHER_HEADING}</p>

                    <div dangerouslySetInnerHTML={{__html: dropboxHelpText}} />

                    <p>
                        <Bootstrap.Button
                            bsSize="small"
                            bsStyle="primary"
                            href={`mailto:${dropboxName}<${dropboxEmail}>?subject=[Journal]`}
                            target="_blank"
                            >
                            {this.props.language.JOURNAL_MAILMATCHER_SEND}
                        </Bootstrap.Button>
                    </p>
                </Bootstrap.Alert>
            );
        }
    }

    containerElement:HTMLDivElement | undefined = undefined;

    render() {
        if (this.props.user === null) {
            return (
                <Loader
                    visible={true}
                    />
            );
        }

        return (
            <div style={BaseStyles.root}>
                <div className="container-fluid">
                    <div
                        className="container"
                        >
                        <div
                            ref={el => this.containerElement = el}
                            >
                        </div>
                    </div>
                </div>
            </div>
        );
    }
}

export default ReduxUtils.reduxContainer<ReduxState, ReduxActions, Props>({
    reduxInitialState,
    configureStore,
    componentClass: JournalsPage,
    reduxActions
})
