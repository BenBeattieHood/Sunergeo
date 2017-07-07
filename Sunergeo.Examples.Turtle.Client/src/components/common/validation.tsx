import * as React from 'react';
import * as _ from 'lodash';
import * as Bootstrap from 'react-bootstrap';
import { Seq } from '../../utils/functional';

export function collate<T>(
    value:T, 
    validators?:Validator<T>[],
    errors?:string[]
    ):string[] {
    const validationResults = validators && _.flatten(_.map(validators, validator => validator(value)));
    const collatedResults = _.filter([
        validationResults,
        errors
    ], x => x !== undefined) as string[][]
    return _.flatten(collatedResults);
}

export const render = (validation:string[]):(JSX.Element | null) => 
    (validation.length === 0)
    ? null
    : <Bootstrap.HelpBlock>
        {_.map(validation, (validationMessage, index) => 
            <div key={index}>{validationMessage}</div>
        )}
    </Bootstrap.HelpBlock>

export interface Validator<T> {
    (value:T): string[]
}

export const requiredString:Validator<string> = (value:string):string[] => 
    _.trimStart(value).length === 0
    ? [ "Can't be blank" ]
    : []