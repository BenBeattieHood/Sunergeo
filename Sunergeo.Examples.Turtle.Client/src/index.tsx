import * as React from 'react';
import { render } from 'react-dom';
import { Router, browserHistory } from 'react-router';
import routes from './routes';
import './styles/css/styles.css'; //Webpack can import CSS files too!

const rootElement = document.getElementById(`react-root`);
if(rootElement == null) throw Error('could not find host element');

render(
    <Router history={browserHistory} routes={routes()} />,
    rootElement
);
