"""Pull node-exporter-full.json out of the latest helm release secret and emit a kubectl patch file that restores it alongside the eco-telemetry dashboard."""
import base64, gzip, json, subprocess, re, yaml

out = subprocess.check_output([
    'kubectl', '--context=kai-server', 'get', 'secret', '-n', 'observability',
    '-l', 'owner=helm,name=grafana', '-o', 'json'
])
secrets = json.loads(out)['items']

def ver(s):
    m = re.search(r'v(\d+)$', s['metadata']['name'])
    return int(m.group(1)) if m else 0

latest = max(secrets, key=ver)
print('using', latest['metadata']['name'])

raw = base64.b64decode(latest['data']['release'])
raw2 = base64.b64decode(raw)
release = json.loads(gzip.decompress(raw2))
docs = list(yaml.safe_load_all(release.get('manifest', '')))
cm = next(d for d in docs if d and d.get('kind') == 'ConfigMap' and d['metadata']['name'] == 'grafana-dashboards-default')
ne = cm['data'].get('node-exporter-full.json')
assert ne, 'no node-exporter-full.json in helm release CM'
print('node-exporter-full.json bytes:', len(ne))

eco = open('/tmp/eco-telemetry-dashboard.json').read()
patch = {'data': {'node-exporter-full.json': ne, 'eco-telemetry.json': eco}}
open('/tmp/cm-restore.json', 'w').write(json.dumps(patch))
print('wrote /tmp/cm-restore.json,', len(json.dumps(patch)), 'bytes')
