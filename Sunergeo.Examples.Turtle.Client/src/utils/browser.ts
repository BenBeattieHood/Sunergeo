import * as _ from 'lodash';

export const getUriQueryParameter = (name:string, url?:string) : string | undefined => {
    if (!url) {
        url = window.location.href;
    }
    name = name.replace(/[\[\]]/g, "\\$&");
    const queryParameterValueRegExp = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)");
    const results = queryParameterValueRegExp.exec(url);
    if (!results) return undefined;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

// From http://stackoverflow.com/a/38080051
// ONLY USE for CSS support
// (see http://www.quirksmode.org/js/support.html and )
export const browserSpec = (():{ name?: ("IE" | "Edge" | "Opera" | "Chrome" | "Safari" | "Firefox"), version?:number } => {
    const userAgent = navigator.userAgent;

    let userAgentMatches = userAgent.match(/(opera|chrome|safari|firefox|msie|trident(?=\/))\/?\s*(\d+)/i) || [];

    const getVersion = (s:string | undefined):number | undefined => s ? Number(s) : undefined;

    if (/trident/i.test(userAgentMatches[1])){
        const userAgentVersionMatches = /\brv[ :]+(\d+)/g.exec(userAgent) || [];
        return {
            name: 'IE',
            version: getVersion(userAgentVersionMatches[1])
        };
    }

    const getBrowserName = (s:string) => {
        switch (s) {
            case 'MSIE':
                return 'IE';
            case 'OPR':
                return 'Opera';

            case 'Firefox':
            case 'Safari':
            case 'IE':
            case 'Chrome':
            case 'Edge':
                return s;

            default:
                return undefined;
        }
    };

    if (userAgentMatches[1] === 'Chrome'){
        let chromeAliasUserAgentMatches = userAgent.match(/\b(OPR|Edge)\/(\d+)/);
        if (chromeAliasUserAgentMatches != null) {
            return {
                name: getBrowserName(chromeAliasUserAgentMatches[1]),
                version: getVersion(chromeAliasUserAgentMatches[2])
            };
        }
    }

    userAgentMatches =
        userAgentMatches[2]
        ? [userAgentMatches[1], userAgentMatches[2]]
        : [navigator.appName, navigator.appVersion, '-?']
        ;

    let userAgentVersionMatches = userAgent.match(/version\/(\d+)/i);
    if (userAgentVersionMatches != null) {
        userAgentMatches.splice(1, 1, userAgentVersionMatches[1]);
    }

    return {
        name: getBrowserName(userAgentMatches[0]),
        version: getVersion(userAgentMatches[1])
    };
})();
