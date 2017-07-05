import * as React from 'react';
import * as _ from 'lodash';

interface Props {
    name?: string,
    label?: string,
    onChange: (value:string)=>void,
    defaultOption?: string,
    value: string,
    error?: string,
    options?: {value:string, text:string}[]
}

export const SelectField = (props:Props) => {
    return (
        <div className='form-group'>
            {props.label ? <label htmlFor={props.name}>{props.label}</label> : null}
            <div className='field'>
                {/* Note, value is set here rather than on the option - docs: https://facebook.github.io/react/docs/forms.html */}
                <select
                    name={props.name}
                    value={props.value}
                    onChange={(event) => props.onChange(event.target.value)}
                    className='form-control'
                    >
                    {props.defaultOption && 
                        <option value=''>{props.defaultOption}</option>
                    }
                    {props.options && 
                    _.map(props.options, option => 
                        <option key={option.value} value={option.value}>
                            {option.text}
                        </option>
                    )}
                </select>
                {props.error && <div className='alert alert-danger'>{props.error}</div>}
            </div>
        </div>
    );
};
