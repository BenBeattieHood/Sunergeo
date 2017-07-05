import * as React from 'react';
import { Route, IndexRoute } from 'react-router';
import { App } from './components/App';
import HomePage from './components/home/HomePage';
import { MonolithConfig } from './utils/environment';

export default (environment:MonolithConfig) =>
    <Route path='*' component={App}>
        <IndexRoute component={() => <HomePage environment={environment}/>} />
    </Route>
