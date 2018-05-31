function [rt60,cfs] = RT60nofile(x,fs)
% Calculates T30 RT60
% modiifed from IR_stats
% http://www.mathworks.co.uk/matlabcentral/fileexchange/42566-impulse-response-acoustic-information-calculator

% octave-band center frequencies

cfs = [31.5 63 125 250 500 1000 2000 4000 8000];

% octave-band filter order

N = 3;

% read in impulse

x = x(:,1);

% limit centre frequencies so filter coefficients are stable

cfs = cfs(cfs>fs/400 & cfs<fs/5);
cfs = cfs(:);

% calculate filter coefficients

a = zeros(length(cfs),(2*N)+1);
b = zeros(length(cfs),(2*N)+1);

for f = 1:length(cfs)
    
    [b(f,:),a(f,:)] = octdsgn(cfs(f),fs,N);
    
end

% empty matrix to fill intergrations

z = zeros([length(cfs) size(x)]);
rt20 = zeros(length(cfs),1);
rt30 = zeros(length(cfs),1);
t0 = find(x(:,1).^2==max(x(:,:).^2));

    for f = 1:length(cfs)
        
        y = filter(b(f,:),a(f,:),x(:,1)); % octave-band filter
        temp = cumtrapz(y(end:-1:1).^2); % energy decay
        z(f,:,1) = temp(end:-1:1);
        rt30(f) = calc_rt(z(f,t0:end,1),fs,30); % estimate RT   
        rt20(f) = calc_rt(z(f,t0:end,1),fs,20); % estimate RT
       
    end
    
 rt60 = horzcat(cfs,rt30,rt20);
  
end

% oct dsgn and calc_rt taken from:
% http://www.mathworks.co.uk/matlabcentral/fileexchange/42566-impulse-response-acoustic-information-calculator

function [B,A] = octdsgn(Fc,Fs,N) 
% OCTDSGN  Design of an octave filter.
%    [B,A] = OCTDSGN(Fc,Fs,N) designs a digital octave filter with 
%    center frequency Fc for sampling frequency Fs. 
%    The filter are designed according to the Order-N specification 
%    of the ANSI S1.1-1986 standard. Default value for N is 3. 
%    Warning: for meaningful design results, center values used
%    should preferably be in range Fs/200 < Fc < Fs/5.
%    Usage of the filter: Y = FILTER(B,A,X). 
%
%    Requires the Signal Processing Toolbox. 
%
%    See also OCTSPEC, OCT3DSGN, OCT3SPEC.

% Author: Christophe Couvreur, Faculte Polytechnique de Mons (Belgium)
%         couvreur@thor.fpms.ac.be
% Last modification: Aug. 22, 1997, 9:00pm.

% References: 
%    [1] ANSI S1.1-1986 (ASA 65-1986): Specifications for
%        Octave-Band and Fractional-Octave-Band Analog and
%        Digital Filters, 1993.

if (nargin > 3) || (nargin < 2)
  error('Invalide number of arguments.');
end
if (nargin == 2)
  N = 3; 
end
if (Fc > 0.70*(Fs/2))
  error('Design not possible. Check frequencies.');
end

% Design Butterworth 2Nth-order octave filter 
% Note: BUTTER is based on a bilinear transformation, as suggested in [1]. 
%W1 = Fc/(Fs/2)*sqrt(1/2);
%W2 = Fc/(Fs/2)*sqrt(2); 
pi = 3.14159265358979;
beta = pi/2/N/sin(pi/2/N); 
alpha = (1+sqrt(1+8*beta^2))/4/beta;
W1 = Fc/(Fs/2)*sqrt(1/2)/alpha; 
W2 = Fc/(Fs/2)*sqrt(2)*alpha;
[B,A] = butter(N,[W1,W2]); 


end

function [rt] = calc_rt(E,fs,RTdB)

ydb = [-5,-5-RTdB]; % dB range for calculating RT

E = 10.*log10(E); % put into dB
E = E-max(E); % normalise to max 0
% E = E(1:find(isinf(E),1,'first')-1); % remove trailing infinite values
IX = find(E<=ydb(1),1,'first'):find(E<=ydb(2),1,'first'); % find ydb x-range

% calculate fit over ydb
x = reshape(IX,1,length(IX));
y = reshape(E(IX),1,length(IX));
p = polyfit(x,y,1);
fit = polyval(p,1:length(E));
fit2 = fit-max(fit);

diff_y = abs(diff(ydb)); % dB range diff
rt = (60/diff_y)*find(fit2<=-diff_y,1,'first')/fs; % estimate RT
if isempty(rt)
    rt = NaN;
end
 
end
