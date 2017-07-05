import * as React from 'react'
import * as _ from 'lodash';
import * as Styles from '../../styles/core';
import { SuggestedInput } from './SuggestedInput';
import * as Environment from '../../utils/environment';
import { FlexBox } from './FlexBox';
import * as Bootstrap from 'react-bootstrap';

namespace BaseStyles {
    export const tag = Styles.compose(
        Styles.Palette.Controls.Tag,
        Styles.Palette.Controls.Clickable,
        Styles.Box.margin(5, 5, 8, 0)
    )
    export const tagGlyph = Styles.compose(
        Styles.Palette.Controls.TagGlyph,
        Styles.Palette.Controls.Clickable
    )
    export const input = Styles.compose(
        Styles.FlexLayout.Flex(),
        {
            minHeight: 30,
            minWidth: 200,
            outline: 'none'
        }
    )
}

interface Props {
    addOnBlur?: boolean,
    className?: string,
    name?: string,
    onChange: (value:string[]) => void,
    value: string[],
    placeholder?: string,
    style?: React.CSSProperties,
    tags?: string[]
}
interface State {
    inputValue: string
}
export class TagsInput extends React.Component<Props, State> {
    readonly ADD_TAG_KEY_CODES = [
        Environment.KeyCodes.Tab,
        Environment.KeyCodes.Enter
    ]
    readonly REMOVE_TAG_KEY_CODES = [
        Environment.KeyCodes.Backspace
    ]

    canCallSetState:boolean = false;
    componentWillMount() {
        this.canCallSetState = true;
        this.attemptSetState({
            inputValue: ''
        });
    }

    componentWillUnmount() {
        this.canCallSetState = false;
    }

    // attemptSetState<K extends keyof State>(f: (prevState: State, props: Props) => Pick<State, K>, callback?: () => any): void {
    //     this.setState(f, callback);
    // }
    attemptSetState<K extends keyof State>(state: Pick<State, K>, callback?: () => any): void {
        if (this.canCallSetState) {
            this.setState(state, callback);
        }
    }

    render() {
        return (
            <div
                ref={(el) => this.rootElement = el}
                className="form-control"
                onClick={this.handleOnClick}
                style={Styles.compose({height: 'auto'}, this.props.style)}
                >
                <FlexBox
                    direction="Horizontal"
                    inline={false}
                    >
                    {_.map(this.props.value, (tag, index) =>
                        <span
                            key={index}
                            className="pu-tag"
                            onClick={() => this.handleOnRemove(index)}
                            style={BaseStyles.tag}
                            >
                            {tag}
                            <sup
                                className="fa fa-times-circle-o"
                                style={BaseStyles.tagGlyph}
                                />
                        </span>
                    )}
                    <SuggestedInput
                        ref={el => this.inputElement = el}
                        value={this.state.inputValue}
                        onChange={this.handleOnChange}
                        onKeyDown={this.handleOnKeyDown}
                        onBlur={this.handleOnBlur}
                        items={this.props.tags || []}
                        placeholder={this.props.placeholder}
                        style={Styles.compose(BaseStyles.input, this.props.value.length > 0 ? Styles.Box.margin(null, null, null, 5) : undefined)}
                        />
                </FlexBox>
            </div>
        )
    }

    private rootElement:HTMLDivElement | undefined = undefined;
    private inputElement:SuggestedInput | undefined = undefined;

    blur() {
        if (this.inputElement) {
            this.inputElement.blur();
        }
    }

    focus() {
        if (this.inputElement) {
            this.inputElement.focus();
        }
    }

    select() {
        if (this.inputElement) {
            this.inputElement.select();
        }
    }

    addTag(tag:string) {
        if (tag !== '') {
            this.props.onChange(this.props.value.concat([tag]));
            this.clearInput();
        }
    }

    clearInput() {
        this.attemptSetState({
            inputValue: ''
        });
    }

    handleOnBlur = () => {
        if (this.props.addOnBlur === undefined || this.props.addOnBlur) {
            this.addTag(this.state.inputValue);
        }
    }

    handleOnKeyDown = (event:{keyCode:number, preventDefault:()=>void, stopPropogation:()=>void, value:string}) => {
        if (!this.state.inputValue && _.find(this.REMOVE_TAG_KEY_CODES, key => key === event.keyCode)) {
            event.preventDefault();
            this.removeTag(this.props.value.length - 1);
        }
    }

    handleOnChange = (value:string, withCommit:boolean) => {
        this.attemptSetState({
            inputValue: value
        });
        if (withCommit) {
            this.addTag(value);
        }
    }

    handleOnClick = (event:React.MouseEvent<HTMLElement>) => {
        if (event.currentTarget === this.rootElement) {
            this.focus();
        }
    }

    handleOnRemove = (tagIndex:number) => {
        this.removeTag(tagIndex);
    }

    removeTag(index:number) {
        const value = this.props.value.concat([]);

        if (index > -1 && index < value.length) {
            value.splice(index, 1);

            this.props.onChange(value)
        }
    }
}
