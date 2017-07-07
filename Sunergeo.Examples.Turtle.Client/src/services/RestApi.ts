import { browserSpec } from '../utils/browser';
import * as Promise from 'bluebird';
import * as Request from 'superagent';
const legacyIESupport = require('superagent-legacyiesupport');

import * as StringUtils from '../utils/stringUtils';

export class RestApiV2 {

    constructor(
        public baseUri:string    //https://apiv2.dc2.pageuppeople.com
        ) {}
    
    private request<TResult>(args:{
        method:(request:Request.SuperAgent<Request.SuperAgentRequest>)=>Request.SuperAgentRequest,
        r?: (result:any) => TResult
    }):Promise<TResult> {
        return new Promise<TResult>((resolve, reject) => {
            let request = args.method(Request);
            if (browserSpec.name === 'IE' && browserSpec.version && browserSpec.version <= 9) {
                request = request.use(legacyIESupport);
            }
            return (
                request
                .set('Accept', 'application/json')      // legacyIESupport (by chance) only supports application/json
                .end((err, res) => {
                    if (err) {
                        reject(new Error(err.description || err.message));
                    }
                    else if (args.r) {
                        if (res == null || res.body == null) {
                            reject(new Error('No response received'));
                        }
                        else {
                            let resBodyKeys = Object.keys(res.body);
                            if (resBodyKeys.length === 1 && resBodyKeys[0] === 'Message') {
                                reject(new Error(res.body.Message));
                            }
                            else {
                                resolve(args.r(res.body));
                            }
                        }
                    }
                    else {
                        (res != null && res.body != null)
                        ? reject(new Error('Unexpected response data received'))
                        : resolve()
                        ;
                    }
                })
            );
        })
    }

    private toAbsoluteUri = (relativeUri:string):string => StringUtils.join('/', this.baseUri, relativeUri);

    public get<TResult>(args:{
        uri:string,
        r: (result:any) => TResult
    }):Promise<TResult> {
        return this.request({
            r:args.r,
            method: request => request.get(this.toAbsoluteUri(args.uri))
        })
    }

    public post<TResult>(args:{
        uri:string,
        form:any,
        r: (result:any) => TResult
    }):Promise<TResult> {
        return this.request({
            r:args.r,
            method: request => request.post(this.toAbsoluteUri(args.uri)).send(args.form)
        })
    }

    public put<TResult>(args:{
        uri:string,
        form:any,
    }):Promise<void> {
        return this.request<void>({
            method: request => request.put(this.toAbsoluteUri(args.uri)).send(args.form)
        })
    }

    public delete(args:{
        uri:string,
        form?:any
    }):Promise<void> {
        return this.request<void>({
            method: request => request.del(this.toAbsoluteUri(args.uri), args.form)
        })
    }
}