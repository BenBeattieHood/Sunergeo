import * as _ from 'lodash';

export namespace Seq {

    export function choose<T, TResult>(
        collection: _.List<T>,
        iteratee?: _.ListIterator<T, TResult | undefined>
    ): TResult[] {
        return _.filter(_.map(collection, iteratee), x => x !== undefined) as TResult[];
    }
}