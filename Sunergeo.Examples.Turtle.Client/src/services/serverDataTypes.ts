export type TurtleId = number

export interface Position {
    x: number,
    y: number
}

export enum Direction {
    North,
    East,
    South,
    West
}

export interface Turtle {
    turtleId: TurtleId,
    positions: Position[],
    direction: Direction,
    isVisible: boolean
}