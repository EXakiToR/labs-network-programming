"""Regenerate the report's tables and figures from retained measurements."""
from pathlib import Path
import json
import math
import csv
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch

REPORT = Path(__file__).resolve().parents[1] / 'report'
DATA = REPORT / 'data'
FIG = REPORT / 'figures'
FIG.mkdir(exist_ok=True)
records = json.loads((DATA/'saved_sessions.json').read_text())
primary = records[1]['tasks']
earlier = records[0]['tasks']
blue, teal, orange, gray = '#3565a8', '#168375', '#c66b30', '#8292a5'
plt.rcParams.update({'font.family':'DejaVu Sans','font.size':12,'axes.titlesize':12,'axes.labelsize':11.5,'xtick.labelsize':10.5,'ytick.labelsize':10.5,'axes.spines.top':False,'axes.spines.right':False,'axes.edgecolor':'#9aa9b9','grid.color':'#dfe5ed','grid.linewidth':.7,'pdf.fonttype':42})

def save(fig, name):
    fig.savefig(FIG/f'{name}.pdf', bbox_inches='tight', facecolor='white')
    fig.savefig(FIG/f'{name}.png', dpi=180, bbox_inches='tight', facecolor='white')
    plt.close(fig)

def format_axes(axes):
    for ax in np.ravel(axes):
        ax.set_ylim(0, ax.get_ylim()[1] * 1.08)
        ax.grid(axis='y',zorder=0)
        ax.set_axisbelow(True)

def worker_chart(task):
    fig,axes=plt.subplots(1,3,figsize=(10.4,3.4))
    for source,label,color,style in [(primary,'16:42 (primary)',blue,'-'),(earlier,'15:43 (repeat)',gray,'--')]:
        rows=source[task];times=np.array([int(r['Elapsed (ms)']) for r in rows]);mem=np.array([int(r['Peak memory (bytes)'])/2**20 for r in rows]);x=np.arange(len(rows))
        for ax,values in zip(axes,[times/1000,times[0]/times,mem]):
            ax.plot(x,values,style,color=color,marker='o',ms=4,lw=1.8,label=label)
            ax.set_xticks(x,[r['Threads'] for r in rows]);ax.set_xlabel('Threads' if task=='1a' else 'Consumers')
    for ax,title,ylabel in zip(axes,['Elapsed time','Speed-up','Peak memory'],['Seconds','T(1) / T(p)','MiB']):
        ax.set_title(title,pad=10);ax.set_ylabel(ylabel)
    axes[1].axhline(1,color='#a6afb8',ls=':',lw=1)
    format_axes(axes);fig.legend(*axes[0].get_legend_handles_labels(),loc='lower center',ncol=2,frameon=False,fontsize=10.5)
    fig.tight_layout(rect=(0,.12,1,1),w_pad=1.5);save(fig,'task'+task)

def table(name,caption,label,headers,rows):
    align='l'+'r'*(len(headers)-1)
    rows=[[f'{int(cell):,}'.replace(',', r'\,') if cell.isdigit() and len(cell)>4 else cell for cell in row] for row in rows]
    out=[r'\begin{table}[H]\centering\small',r'\caption{'+caption+'}'+r'\label{'+label+'}',r'\setlength{\tabcolsep}{5pt}',r'\begin{tabular}{@{}'+align+r'@{}}\toprule',' & '.join(headers)+r'\\\midrule']
    out += [' & '.join(row)+r'\\' for row in rows]
    out += [r'\bottomrule\end{tabular}\end{table}']
    (DATA/f'{name}.tex').write_text('\n'.join(out)+'\n')

for task in ['1a','1c']:
    rows=primary[task];t1=int(rows[0]['Elapsed (ms)'])
    table('task'+task+'_table',f'Task {task}: primary session at 16:42.' + (' The consumer count excludes the dedicated reader.' if task == '1c' else ''),f'tab:task{task}', ['Threads' if task=='1a' else 'Consumers','Elapsed (ms)','Speed-up','Peak RSS (MiB)'],[[r['Threads'],r['Elapsed (ms)'],f"{t1/int(r['Elapsed (ms)']):.3f}",f"{int(r['Peak memory (bytes)'])/2**20:.3f}"] for r in rows])
    worker_chart(task)

b = sorted([json.loads(p.read_text()) for p in DATA.glob('task1b-*.json')],key=lambda r:(r['workers'],{'Unsynchronized':0,'Locked':1,'LocalCounters':2}[r['version']],r['run']))
b8=[r for r in b if r['workers']==8]
table('task1b_table','Supplementary Task 1b: eight threads, with file loading excluded from counting time.','tab:task1b',['Version / run','Time (ms)','Negative','Positive','Zero','Total','Correct'],[[r['version']+' '+str(r['run']),f"{r['elapsed_ms']:.3f}",str(r['negative']),str(r['positive']),str(r['zero']),str(r['negative']+r['positive']+r['zero']),'Yes' if r['correct'] else 'No'] for r in b8])
fig,axes=plt.subplots(1,2,figsize=(10.4,3.5));x=np.arange(len(b8));colors=[orange if not r['correct'] else teal for r in b8];labels=['Unsafe 1','Unsafe 2','Unsafe 3','Locked','Local']
axes[0].bar(x,[r['elapsed_ms'] for r in b8],color=colors,width=.63)
axes[0].set_ylabel('Counting time (ms)');axes[0].set_title('Time by version / run')
for i,r in enumerate(b8):axes[0].annotate(f"{r['elapsed_ms']:.0f}",(i,r['elapsed_ms']),xytext=(0,4),textcoords='offset points',ha='center',fontsize=10)
axes[0].set_ylim(0,max(r['elapsed_ms'] for r in b8)*1.18)
axes[1].bar(x,[(r['negative']+r['positive']+r['zero'])/1e6 for r in b8],color=colors,width=.63)
axes[1].axhline(50,color=blue,ls='--',lw=1.5);axes[1].text(0,51,'Required: 50 million',color=blue,fontsize=10)
axes[1].set_ylim(0,57);axes[1].set_ylabel('Total counted (millions)');axes[1].set_title('Correctness before performance')
for ax in axes:ax.set_xticks(x,labels);ax.tick_params(axis='x',labelsize=10)
format_axes(axes);fig.tight_layout();save(fig,'task1b_versions')
fig,axes=plt.subplots(1,3,figsize=(10.4,3.5))
for version,color,style in [('LocalCounters',teal,'-'),('Locked',blue,'-'),('Unsynchronized',orange,'--')]:
    vals=[]
    for workers in [1,8]:
        group=[r for r in b if r['version']==version and r['workers']==workers]
        vals.append((np.mean([r['elapsed_ms'] for r in group]),max(r['peak_memory_bytes'] for r in group)/2**20))
    times=np.array([v[0] for v in vals]);mem=[v[1] for v in vals]
    for ax,y in zip(axes,[times,times[0]/times,mem]):ax.plot([0,1],y,style,color=color,marker='o',ms=5,label=version+(' (incorrect at 8)' if version=='Unsynchronized' else ''))
for ax,title,ylabel in zip(axes,['Counting time','Own-baseline speed-up','Process peak memory'],['Milliseconds','T(1) / T(p)','MiB']):
    ax.set_xticks([0,1],['1','8']);ax.set_xlabel('Threads');ax.set_title(title,pad=10);ax.set_ylabel(ylabel)
axes[1].axhline(1,color=gray,ls=':',lw=1);format_axes(axes)
fig.legend(*axes[0].get_legend_handles_labels(),loc='lower center',ncol=3,frameon=False,fontsize=9.5)
fig.tight_layout(rect=(0,.14,1,1));save(fig,'task1b_scaling')

usb=[json.loads((DATA/f'usb-cold-{p}.json').read_text()) for p in [1,2,4]]
times=np.array([r['elapsed_ms'] for r in usb]);mem=np.array([r['peak_memory_bytes']/2**20 for r in usb]);speeds=times[0]/times
assert all(r['correct'] and r['cold_cache'] for r in usb)
table('task2_table','Task 2: separate physical USB reconnect before every run; Spotlight disabled.','tab:task2',['Threads','Elapsed (ms)','Speed-up','Peak RSS (MiB)','Total counted'],[[str(r['workers']),f"{r['elapsed_ms']:.3f}",f"{speeds[i]:.3f}",f"{mem[i]:.3f}",str(r['negative']+r['positive']+r['zero'])] for i,r in enumerate(usb)])
fig,axes=plt.subplots(1,3,figsize=(10.4,3.4))
for ax,y,title,ylabel in zip(axes,[times/1000,speeds,mem],['Cold-cache USB time','USB speed-up','Peak memory'],['Seconds','T(1) / T(p)','MiB']):
    ax.plot([0,1,2],y,'-o',color=teal,lw=2,ms=5);ax.set_xticks([0,1,2],['1','2','4']);ax.set_xlabel('Threads');ax.set_title(title,pad=10);ax.set_ylabel(ylabel)
axes[1].axhline(1,color=gray,ls=':',lw=1);format_axes(axes);fig.tight_layout();save(fig,'task2')
interpretation='Additional threads improve the measured throughput, but the improvement is sublinear: four workers do not deliver four times the one-worker performance.' if speeds[2]>1 else 'Additional threads do not improve the four-thread result over the one-thread baseline in this experiment; the shared storage and coordination costs dominate.'
macros={'USBOneMs':f'{times[0]:.3f}','USBTwoMs':f'{times[1]:.3f}','USBFourMs':f'{times[2]:.3f}','USBSpeedTwo':f'{speeds[1]:.3f}','USBSpeedFour':f'{speeds[2]:.3f}','USBvsSSD':f'{times[0]/3876:.3f}','USBOneMBs':f'{369439934/times[0]/1000:.2f}','USBScalingInterpretation':interpretation}
(DATA/'result_macros.tex').write_text('\n'.join('\\newcommand{\\'+key+'}{'+value+'}' for key,value in macros.items())+'\n')
with (DATA/'new_measurements.csv').open('w') as stream:
    fields=['task','workers','version','run','elapsed_ms','peak_memory_bytes','negative','positive','zero','correct','cold_cache']
    writer=csv.DictWriter(stream,fields,extrasaction='ignore');writer.writeheader();writer.writerows(b+usb)

fig,ax=plt.subplots(figsize=(10.0,2.5));ax.axis('off')
boxes=[(.01,.43,.17,.39,'numbers.txt\n50 million lines'),(.24,.43,.18,.39,'1 producer\nReadLine + batches'),(.48,.43,.22,.39,'Bounded queue\n100 batches maximum'),(.76,.43,.23,.39,'p consumers\nparse + local counts')]
for x,y,w,h,text in boxes:
    ax.add_patch(FancyBboxPatch((x,y),w,h,boxstyle='round,pad=0.01',facecolor='#f1f5fb',edgecolor=blue,lw=1.2));ax.text(x+w/2,y+h/2,text,ha='center',va='center',fontsize=11.5)
for start,end in [(.18,.24),(.42,.48),(.70,.76)]:ax.annotate('',xy=(end-.01,.63),xytext=(start+.01,.63),arrowprops={'arrowstyle':'->','color':blue,'lw':1.6})
ax.text(.5,.23,'One batch = 10,000 lines. The producer blocks when full; consumers block when empty.',ha='center',fontsize=11)
ax.text(.5,.04,'Queued lines: at most 1,000,000. Active producer/consumer batches add further live lines.',ha='center',fontsize=10.5)
fig.tight_layout();save(fig,'pipeline')
print('Generated all report tables, macros, six vector chart/diagram figures, PNG previews, and the new measurement CSV.')
