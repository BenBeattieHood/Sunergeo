import * as React from 'react';
import * as Styles from '../../styles/core';
import * as Validation from './validation';
import * as Bootstrap from 'react-bootstrap';

namespace BaseStyles {
    export const input = Styles.compose(
        Styles.Palette.Controls.TextInput,
        Styles.Palette.Controls.InputControl,
        {
            display: 'block',
            width: '100%'
        }
    )
}

interface Props {
    name?: string,
    label?: string,
    onChange: (value:string)=>void,
    placeholder?: string,
    value: string,
    errors?: string[],
    styles?: {
        label?: React.CSSProperties,
        input?: React.CSSProperties
    },
    validators?: Validation.Validator<string>[]
}
export const TextField = (props:Props) => {
    const validation = Validation.collate(props.value, props.validators, props.errors);
    const validationState =
        validation.length > 0
        ? "error"
        : undefined
        ;

    return (
        <Bootstrap.FormGroup
            validationState={validationState}
            >
            {props.label && 
                <Bootstrap.ControlLabel 
                    htmlFor={props.name} 
                    style={props.styles && props.styles.label}
                    >
                    {props.label}
                </Bootstrap.ControlLabel>
            }
            <input
                type='text'
                name={props.name}
                placeholder={props.placeholder}
                value={props.value}
                onChange={(event) => props.onChange(event.target.value)}
                className="form-control"
                style={props.styles && props.styles.input}
                />
            {Validation.render(validation)}
        </Bootstrap.FormGroup>
    );
};