import * as ServerDataStructures from '../../services/serverDataTypes';

export type AlertMessage = 
    Success
    | Info
    | Warning
    | Error

// types are string literal rather than enum because they may be persisted and rehydrated from client state

export interface Success {
    type: 'Success',
    message: string
}

export interface Info {
    type: 'Info',
    message: string
}

export interface Warning {
    type: 'Warning',
    message: string
}

export interface Error {
    type: 'Error',
    message: string
}

export interface ParentNode { 
    children?: React.ReactNode 
}