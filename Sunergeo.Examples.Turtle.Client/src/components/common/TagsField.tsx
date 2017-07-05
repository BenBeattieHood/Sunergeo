import * as React from 'react';
import * as Styles from '../../styles/core';
import * as Validation from './validation';
import * as Bootstrap from 'react-bootstrap';
import { TagsInput } from './TagsInput';

namespace BaseStyles {
    export const input:React.CSSProperties = Styles.compose(
        {
            display: 'block'
        }
    )
}

interface Props {
    name?: string,
    label?: string,
    onChange: (value:string[]) => void,
    placeholder?: string,
    value: string[],
    errors?: string[],
    styles?: {
        label?: React.CSSProperties,
        input?: React.CSSProperties
    },
    tags?: string[],
    validators?: Validation.Validator<string[]>[]
}
export const TagsField = (props:Props) => {
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
            <TagsInput
                name={props.name}
                placeholder={props.placeholder}
                value={props.value}
                onChange={value => props.onChange(value)}
                style={props.styles && props.styles.input}
                tags={props.tags}
                />
            {Validation.render(validation)}
        </Bootstrap.FormGroup>
    );
};
