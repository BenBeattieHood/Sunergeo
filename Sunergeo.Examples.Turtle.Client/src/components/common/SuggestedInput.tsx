import * as React from 'react';
import * as _ from 'lodash';
import * as Styles from '../../styles/core';
import * as Bootstrap from 'react-bootstrap';
import { KeyCodes } from '../../utils/environment';

namespace BaseStyles {
    export const itemsList = Styles.compose(
        Styles.Palette.Controls.List,
        Styles.Box.borderWidth(0, null, null, null),
        Styles.Box.position(33, 0, null, 0),
        {
            position: 'absolute',
            zIndex: 1,
            maxHeight: 'calc(100vh - 630px)',
            overflowY: 'auto'
        }
    )
    export const item = Styles.compose(
        Styles.Palette.Controls.ListItem,
        Styles.Palette.Controls.Clickable
    )
    export const itemActive = Styles.compose(
        item,
        Styles.Palette.Controls.ListItemActive,
        {
            fontWeight: 'bold'
        }
    )
}

interface Props {
    value: string,
    onChange: (value:string, withCommit:boolean)=>void,
    onKeyDown?: (e:{keyCode:number, preventDefault:()=>void, stopPropogation:()=>void, value:string}) => void,
    onBlur?: () => void,
    onFocus?: () => void,
    items: string[],
    placeholder?: string,
    inputRef?: React.Ref<HTMLInputElement>,
    className?: string,
    style?: React.CSSProperties
}

interface State {
    selectedIndex: number | null,
    hasFocus: boolean
}

export class SuggestedInput extends React.Component<Props, State> {
    componentWillMount() {
        this.setState({
            selectedIndex: null,
            hasFocus: false
        })
    }

    componentWillReceiveProps(nextProps:Props) {
        if (!_.eq(nextProps.items, this.props.items)) {
            this.setState({
                selectedIndex: nextProps.items.length ? 0 : null
            })
        }
    }

    shouldShowItemsList = () =>             // show if:
        this.props.value                    // the user has typed min 1 char
        && this.props.value.length > 0      // ..
        && this.state.hasFocus              // and the control has focus
        && this.getVisibleItems().length > 0      // and there are items to show
        && !(                                   // and there is not
            this.props.items.length === 1       // only one item
            && this.props.items[0] === this.props.value // whose value is exactly the same as what's already typed in
        )

    getVisibleItems = () => _.filter(this.props.items, tag => this.props.value === "" || tag.indexOf(this.props.value) >= 0)

    private inputElement:HTMLInputElement | undefined = undefined;

    render() {
        return (
            <div
                className={this.props.className}
                style={Styles.compose(this.props.style,
                    Styles.FlexLayout.BlockContainerHorizontal,
                    {
                        position: 'relative'
                    }
                )}
                >
                <input
                    ref={el => this.inputElement = el}
                    type="text"
                    onKeyDown={(event:React.KeyboardEvent<HTMLInputElement>) => this.onKeyDown(event)}
                    value={this.props.value}
                    onChange={(e:any) => this.handleOnChange(e.currentTarget.value, false)}
                    onFocus={() => this.onFocus()}
                    onBlur={(event) => this.onBlur()}
                    placeholder={this.props.placeholder}
                    style={Styles.compose(
                        Styles.Box.borderWidth(0),
                        Styles.FlexLayout.Flex(),
                        {
                            backgroundColor: 'transparent',
                            color: 'inherit',
                            fill: 'inherit',
                            fontSize:'inherit',
                            lineHeight: 'inherit',
                            outline: 'none',

                            minWidth: 120
                        }
                    )}
                    />
                {this.shouldShowItemsList() &&
                    <div
                        style={BaseStyles.itemsList}
                        >
                        {_.map(this.getVisibleItems(), (item, index) => {
                            return (
                                <div
                                    key={index}
                                    style={index === (this.state.selectedIndex || 0) ? BaseStyles.itemActive : BaseStyles.item}
                                    onClick={() => this.handleOnChange(item, true)}
                                    onMouseEnter={() => this.setState({selectedIndex: index})}
                                    >
                                    {item}
                                </div>
                            );
                        })}
                    </div>
                }
            </div>
        );
    }

    handleOnChange(value:string, withCommit:boolean):void {
        this.setState({
            selectedIndex: null
        });
        this.props.onChange(value, withCommit);
    }

    focus() {
        if (this.inputElement) {
            this.inputElement.focus();
        }
    }

    blur() {
        if (this.inputElement) {
            this.inputElement.blur();
        }
    }

    select() {
        if (this.inputElement) {
            this.inputElement.select();
        }
    }

    onFocus = _.debounce(() => {
        this.setState({hasFocus: true});
        this.props.onFocus && this.props.onFocus();
    }, 50)

    onBlur = _.debounce(() => {
        this.setState({hasFocus: false});
        this.props.onBlur && this.props.onBlur();
    }, 200)

    onKeyDown(e:React.KeyboardEvent<HTMLInputElement>) {
        const visibleItems = this.getVisibleItems();

        let eventPropogationCancelled = false;
        const cancelEventPropogation = () => {
            eventPropogationCancelled = true;
            e.preventDefault();
            e.stopPropagation();
        }

        switch (e.keyCode) {
            case KeyCodes.PageUp:
            case KeyCodes.UpArrow:
                if (visibleItems.length > 0) {
                    this.setState({
                        selectedIndex: (() => {
                            switch (this.state.selectedIndex || 0) {
                                case 0:
                                    return 0;
                                default:
                                    return (this.state.selectedIndex || 0) - (e.keyCode === KeyCodes.PageUp ? 7 : 1);
                            }
                        })()
                    });
                    cancelEventPropogation();
                }
                break;

            case KeyCodes.PageDown:
            case KeyCodes.DownArrow:
                if (visibleItems.length > 0) {
                    this.setState({
                        selectedIndex: (() => {
                            switch (this.state.selectedIndex || 0) {
                                case visibleItems.length - 1:
                                    return this.state.selectedIndex;
                                default:
                                    return (this.state.selectedIndex || 0) + (e.keyCode === KeyCodes.PageDown ? 7 : 1);
                            }
                        })()
                    });
                    cancelEventPropogation();
                }
                break;

            case KeyCodes.Escape:
                this.setState({
                    selectedIndex: null
                });
                cancelEventPropogation();
                break;

            case KeyCodes.Enter:
                if (visibleItems.length > 0) {
                    const visibleItems = this.getVisibleItems();
                    this.handleOnChange(visibleItems[this.state.selectedIndex || 0], true);
                    cancelEventPropogation();
                }
                else {
                    this.handleOnChange(this.props.value, true);
                    cancelEventPropogation();
                }
                break;
        }

        this.props.onKeyDown && this.props.onKeyDown({
            keyCode:e.keyCode,
            preventDefault:e.preventDefault,
            stopPropogation:e.stopPropagation,
            value: e.currentTarget.value
        });
    }
}
