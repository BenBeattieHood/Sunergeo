export type TurtleId = number

export interface Position {
    X: number,
    Y: number
}

export enum Direction {
    North,
    East,
    South,
    West
}

export interface Turtle {
    turtleId: TurtleId,
    position: Position,
    direction: Direction
}