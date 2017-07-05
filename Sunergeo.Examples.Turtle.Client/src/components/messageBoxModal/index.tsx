import * as React from 'react'
import * as Bootstrap from 'react-bootstrap';

interface Props {
    show: boolean,
    title: string,
    message: string,
    onCancel?: ()=>void,
    onAccept?: ()=>void,
}
export const MessageBox = (props:Props) => {
    return (
        <Bootstrap.Modal
            show={props.show}
            onHide={() => props.onCancel && props.onCancel()}
            >
            <Bootstrap.Modal.Header>
                <Bootstrap.Modal.Title>
                    {props.title}
                </Bootstrap.Modal.Title>
            </Bootstrap.Modal.Header>

            <Bootstrap.Modal.Body>
                {props.message}
            </Bootstrap.Modal.Body>

            <Bootstrap.Modal.Footer>
                {props.onCancel &&
                    <Bootstrap.Button>
                        Cancel
                    </Bootstrap.Button>
                }
                {props.onAccept &&
                    <Bootstrap.Button
                        bsStyle="primary"
                        onClick={props.onAccept}
                        >
                        OK
                    </Bootstrap.Button>
                }
            </Bootstrap.Modal.Footer>
        </Bootstrap.Modal>
    );
}
