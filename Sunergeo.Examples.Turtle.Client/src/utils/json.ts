export namespace Json {
    /**
      * Converts a JavaScript Object Notation (JSON) string into an object.
      * @param text A valid JSON string.
      * @param reviver A function that transforms the results. This function is called for each member of the object.
      * If a member contains nested objects, the nested objects are transformed before the parent object is.
      */
    export function parse<T>(text: string, reviver?: (key: keyof(T), value: any) => any): T {
        return JSON.parse(text, reviver);
    }

    /**
      * Converts a JavaScript value to a JavaScript Object Notation (JSON) string.
      * @param value A JavaScript value, usually an object or array, to be converted.
      * @param replacer A function that transforms the results or an array of strings and numbers that acts as a approved list for selecting the object properties that will be stringified.
      * @param space Adds indentation, white space, and line break characters to the return-value JSON text to make it easier to read.
      */
    export function stringify<T>(
        value: T, 
        replacer?: 
            ((key: keyof(T), value: any) => any) 
            | ((number | string)[]) 
            | null,
        space?: string | number
    ): string {
        return JSON.stringify(value, replacer as any, space);
    }
}