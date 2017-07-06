import { TurtleId, Turtle, Direction, Position } from './serverDataTypes';
import * as Promise from 'bluebird';
import { RestApiV2 } from './RestApi';

const noop = (_:any) => { return; }

export class TurtleApi {

    private restApi:RestApiV2;
    constructor(
        baseUri:string,    //https://apiv2.dc2.pageuppeople.com
        ) 
    {
        this.restApi = new RestApiV2(baseUri);
    }
    
    get = (id:TurtleId) =>  
    this.restApi.get({
        uri: `/api/turtle/${id}`,
        r: result => result as Turtle
    })
    
    // getAll = () => 
    // this.restApi.get({
    //     uri: `/`,
    //     r: result => {
    //        return [
    //         {
    //             turtleId: "1",
    //             direction: Direction.North,
    //             positions: [{
    //                 x: 1,
    //                 y: -1
    //             }],
    //             isVisible: true
    //         },
    //         {
    //             turtleId: "2",
    //             direction: Direction.North,
    //             positions: [{
    //                 x: 1,
    //                 y: -1
    //             }],
    //             isVisible: true
    //         }
    //        ]
    //     }
    // })

    getAll = () => {
            return new Promise((resolve,reject) => {
                resolve ([
                    {
                        turtleId: "1",
                        direction: Direction.North,
                        positions: [{
                            x: 1,
                            y: -1
                        }],
                        isVisible: true
                    },
                    {
                        turtleId: "2",
                        direction: Direction.North,
                        positions: [{
                            x: 1,
                            y: -1
                        }],
                        isVisible: true
                    }
                ]);
            })
    }

    // getAll = () => 
    // this.restApi.get({
    //     uri: `/api/turtles`,
    //     r: result => result as Turtle[]
    // })

    create = ():Promise<TurtleId> => 
    this.restApi.post({
        uri: `/api/turtle/create`,
        form: {},
        r: result => result as TurtleId
    })

    turnLeft = (turtleId:TurtleId):Promise<void> => 
    this.restApi.post({
        uri: `/api/turtle/${turtleId}/turn-left`,
        form: {},
        r: noop
    })

    turnRight = (turtleId:TurtleId):Promise<void> => 
    this.restApi.post({
        uri: `/api/turtle/${turtleId}/turn-right`,
        form: {},
        r: noop
    })

    moveForwards = (turtleId:TurtleId):Promise<void> => 
    this.restApi.post({
        uri: `/api/turtle/${turtleId}/move-forwards`,
        form: {},
        r: noop
    })

    setVisibility = (turtleId:TurtleId, isVisible:boolean):Promise<void> => 
    this.restApi.post({
        uri: `/api/turtle/${turtleId}/set-visibility/${isVisible}`,
        form: {},
        r: noop
    })
}
