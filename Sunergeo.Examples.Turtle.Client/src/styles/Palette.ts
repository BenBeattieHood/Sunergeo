import * as React from 'react';
import { compose } from './Compose';
import { Box } from './Box';

namespace Colors {
    export const DefaultContent = '#4A4A4A';
    export const TitleContent = '#444';
    export const InputBorder = '#ddd';
    export const InputBackground = '#fff';
    export const InputContent = '#333';
    export const CardTitle = '#0d6083';
    export const CardContainerBackground = '#F0F0F0';
    export const CardBorder = '#ddd';
    export const CardBackground = '#fff';
    export const ActiveItemBackground = '#ededed';
    export const SelectedItemBackground = '#ddd';
    export const ImportantUiElementBackground = '#ffa500';
    export const ImportantUiElementText = '#fff';
    export const TagBackground = '#939597';
    export const TagContent = '#fff';
    export const AttachmentBackground = '#ddd';
    export const AttachmentBorder = '#e3e3e3';
    export const AttachmentContent = '#333';
}

export namespace Controls {
    export const Base:React.CSSProperties = {
        fontFamily: 'Open Sans',
        fontSize: '1em',
        lineHeight: '1.5em',
        color: Colors.DefaultContent,
        fill: Colors.DefaultContent
    }
    export const Title:React.CSSProperties = {
        color: Colors.TitleContent,
        fill: Colors.TitleContent,
        fontSize: '20px',
        lineHeight: '1.1em'
    }
    export const CardContainer:React.CSSProperties = {
        backgroundColor: Colors.CardContainerBackground,
        color: Colors.DefaultContent,
        fill: Colors.DefaultContent
    }
    export const CardTitle:React.CSSProperties = {
        fontSize: '18px',
        color: Colors.CardTitle,
        fill: Colors.CardTitle
    }
    export const Card:React.CSSProperties = compose(
        Box.borderColor(Colors.CardBorder),
        Box.borderStyle('solid'),
        Box.borderWidth(1),
        {
            boxShadow: `0 1px 3px 0 rgba(0,0,0,.11)`,
            backgroundColor: Colors.CardBackground
        }
    )
    export const ImportantButton:React.CSSProperties = compose(
        {
            backgroundColor: Colors.ImportantUiElementBackground,
            color: Colors.ImportantUiElementText,
            fill: Colors.ImportantUiElementText
        }
    )
    export const List:React.CSSProperties = compose(
        Box.borderColor(Colors.InputBorder),
        Box.borderStyle('solid'),
        Box.borderWidth(0, 1, 1, 1),
        {
            backgroundColor: Colors.CardBackground,
            color: Colors.InputContent,
            fill: Colors.InputContent
        }
    )
    export const ListItem:React.CSSProperties = compose(
        Box.borderColor(Colors.InputBorder),
        Box.borderStyle('solid'),
        Box.borderWidth(1, 0, 0, 0),
        Box.padding(8, 10),
        {
        }
    )
    export const ListItemActive:React.CSSProperties = compose(
        ListItem,
        {
            backgroundColor: Colors.ActiveItemBackground
        }
    )
    export const ListItemSelected:React.CSSProperties = compose(
        ListItem,
        {
            backgroundColor: Colors.SelectedItemBackground
        }
    )
    export const InputControl:React.CSSProperties = compose(
        Box.borderColor(Colors.InputBorder),
        Box.borderStyle('solid'),
        Box.borderWidth(1),
        Box.padding(6, 12),
        {
            backgroundColor: Colors.InputBackground,
            color: Colors.InputContent,
            fill: Colors.InputContent
        }
    )
    export const TextInput:React.CSSProperties = compose(
        InputControl,
        Box.borderWidth(0),
        {
        }
    )

    export const Clickable:React.CSSProperties = compose(
        {
            cursor: 'pointer'
        }
    )
    
    export const Tag:React.CSSProperties = compose(
        Box.padding(5, 8),
        {
            backgroundColor: Colors.TagBackground,
            color: Colors.TagContent,
            fill: Colors.TagContent,
            borderRadius: 3,
            display: 'inline-block',
            position: 'relative',
            fontWeight: 'normal',
            wordWrap: 'break-word',
            maxWidth: '100%',
        }
    )
    export const TagGlyph:React.CSSProperties = compose(
        Box.position(-5, -5, null, null),
        Box.borderColor(Colors.InputBackground),
        Box.borderWidth(1),
        Box.borderStyle('solid'),
        {
            backgroundColor: Colors.TagContent,
            borderBottomLeftRadius: '50%',
            borderTopLeftRadius: '50%',
            borderBottomRightRadius: '50%',
            borderTopRightRadius: '50%',
            position: 'absolute',
            textAlign: 'center',
            color: '#999',
            fontSize: '13px'
        }
    )
    export const Attachment:React.CSSProperties = compose(
        Box.padding(4, 10),
        Box.borderColor(Colors.AttachmentBorder),
        Box.borderStyle('solid'),
        Box.borderWidth(1),
        {
            backgroundColor: Colors.AttachmentBackground,
            color: Colors.AttachmentContent,
            fill: Colors.AttachmentContent,
            borderRadius: '2px',
            fontWeight: 'normal'
        }
    )
}