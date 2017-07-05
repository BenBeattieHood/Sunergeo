import * as _ from 'lodash';

export const join = (separator:string, ...values:string[]) =>
    values.length === 0 ? "" :
    _.reduce<string, string>(
        _.tail(values),
        (state, next) => next ? `${state}${separator}${_.trimStart(next, separator)}` : state,
        _.trimEnd(values[0], separator)
    )
