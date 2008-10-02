#include "Signal.h"

SIGNALBAND::SIGNALBAND()
{
  reset();
}

void SIGNALBAND::reset()
{
  for(int i=0;i<NSAMPLES;i++)
  {
    onset        [i] = 0;
    onsetStraight[i] = 0;
    onsetCut     [i] = 0;
  }

  for(int i=0;i<NRESONANCE;i++)
  {
    resonance      [i] = 0;
    resonancePeaks [i] = 0;
  }

  onsetMean      =  0;
  count          =  0;
  countInc       =  0;
  interval       =  0;
  energy         =  0;

}

void SIGNALBAND::update(int interval)
{
  this->interval = interval;

  energy = onset[count];

  for(int i=0;i<NSAMPLES;i++)
	onsetStraight[i] = onset[((count - i) + NSAMPLES) % NSAMPLES];

  onsetMean -= onset[countInc] / NSAMPLES;
  onsetMean += onset[count]    / NSAMPLES;

  for(int i=0;i<NSAMPLES;i++)
  {
	if(onsetStraight[i] > onsetMean)
	  onsetCut[i] = onsetStraight[i];
	else
	  onsetCut[i] = 0;
  }

}

bool SIGNALBAND::localMaximum()
{
  double value = onsetCut[interval];

  if(value < MINIMUMONSET)
	return false;

  for(int i=0;i<AREA;i++)
  if((onsetCut[interval-i] > value) || (onsetCut[interval + i] > value))
	return false;

  return true;

}

//--------------------------------------------------------------------//

Ctrl::Ctrl()
{
  band           = new double*[NBANDS];
  resonance      = new double*[NBANDS];
  resonancePeaks = new double*[NBANDS];

  chain = new CHAIN*[NBANDS];


  for(int i=0;i<NBANDS;i++)
	band[i] = NULL;

  reset();

  delay          = 0;
  signal         = 0;
  adjust         = 0;
  targetBpm      = 0;
  targetInterval = 0;

}

void Ctrl::reset()
{
  resetSignal = true; 
  phase       =-1;
  counter     = 0;
  currentBeat = false;
  period      = 0;
  bpm         = 0;
  beat        = false;

  for(int i=0;i<NSAMPLES;i++)
  {
	probability [i] = 0;
	silence     [i] = 0;
  }

}

void Ctrl::setSignal (int value)
{
  signal = value;
}

void Ctrl::setAdjust (int value)
{
  adjust = value;
}

void Ctrl::setDelaySlider (float value)
{
  int proposedValue = (int)(value * 100);

  setDelay(proposedValue);
}

void Ctrl::setDelay (int value)
{
  if((value < (NSAMPLES-10)) && (value >= 0))
	delay = value;

  if(value < 0)
	delay = 0;

  if(value >= NSAMPLES)
	delay = NSAMPLES;
}

void Ctrl::setTargetBpm (int value)
{
  if(value == 0)
  {
    targetBpm      = 0;
	targetInterval = 0;
	return;
  }

  if(value < MINBPM || value >= MAXBPM)
	return;

  int tmpInterval = (int)((60. / value) / SECPERFRAME); //bpm to interval

  if(tmpInterval >= MININTERVAL && tmpInterval < MAXINTERVAL)
  {
	targetBpm      = value;
	targetInterval = tmpInterval;
  }

}

void Ctrl::update(int counter,int beat,int topInterval,double probability,double silence)
{
  if(topInterval <= 0)
	bpm = 0;
  else
    bpm = (int)((1. / (topInterval * SECPERFRAME)) * 60.0);
  
  this->counter = counter;

  //this->beat = beat;

  if(beat)
	this->beat = true;

  this->probability [counter] = probability;
  this->silence     [counter] = silence;

  period = (int)(!((bool)(counter % FPSINT)));

  if(beat)
  {
    interval = topInterval;
	phase    = 0;
  }

  if(!beat)
  {
	if(interval > 0)
	 phase += (1. / interval);
	else
	 phase = -1;

	if(phase >= 1)
	  phase = 0;
  }

}

float Ctrl::getDelayInSec()
{
  return (float)(delay * SECPERFRAME);  
}

int Ctrl::getBeat()
{
  static int count = 100;

  if(beat)
  {
    beat  = false;
    count = 0;
  }

  if(count < 2)
  {
    ++count;
	return true;
  }

  return false;

}

double Ctrl::getPhase()
{
  return phase; 
}

int Ctrl::getBpm()
{
  return bpm; 
}

double Ctrl::getProbability()
{
  return probability[((counter - delay) + NSAMPLES) % NSAMPLES];
}

double Ctrl::getSilence()
{
  return silence[((counter - delay) + NSAMPLES) % NSAMPLES];
}

double Ctrl::getBand0()
{
  if(band[0] != NULL)
    return band[0][delay];

  return 0;
}

double Ctrl::getBand1()
{
  if(band[1] != NULL)
    return band[1][delay];

  return 0;
}

double Ctrl::getBand2()
{
  if(band[2] != NULL)
    return band[2][delay];

  return 0;
}

double Ctrl::getBand3()
{
  if(band[3] != NULL)
    return band[3][delay];

  return 0;
}

//--------------------------------------------------------------------//

Signal::Signal()
{
  in   = NULL;
  ctrl = NULL;

  onset          = new double*[NBANDS];
  onsetStraight  = new double*[NBANDS];
  onsetCut       = new double*[NBANDS];
  resonance      = new double*[NBANDS];
  resonancePeaks = new double*[NBANDS];
  cover          = new int*   [NBANDS];

  for(int i=0;i<NBANDS;i++)
  {
	onset          [i] = band[i].onset;
	onsetStraight  [i] = band[i].onsetStraight;
	onsetCut       [i] = band[i].onsetCut;
	resonance      [i] = band[i].resonance;
	resonancePeaks [i] = band[i].resonancePeaks;
	cover          [i] = band[i].cover;
  }

  reset();
}

Signal::~Signal()
{
  in = NULL;

  delete onset;
  delete onsetStraight;
  delete onsetCut;
  delete resonance;
  delete cover;
}

void Signal::init(Ctrl *ctrl,CHAIN chain[][NINTERVAL])
{
  this->ctrl = ctrl;

  if(ctrl)
  {   
	if(ctrl)
	for(int i=0;i<NBANDS;i++)
	{
 	 ctrl->band[i]           = onsetStraight[i];
	 ctrl->resonance[i]      = resonance[i];
	 ctrl->resonancePeaks[i] = resonancePeaks[i];
	 ctrl->chain[i]          = chain[i];
	}

    ctrl->reset();
  }
}

void Signal::reset()
{
  for(int i=0;i<NBANDS;i++)
    band[i].reset();

  for(int i=0;i<NSAMPLES;i++)
	freq[i] = 0;

  for(int i=0;i<NBANDS;i++)
  {
	interval [i] = 0;
    weight   [i] = 0;
  }

  count            =  0;
  countInc         =  1;
  top              = -1;
  silence          =  0;
  probability      =  0;
  topInterval      =  0;
  beat             =  0;
  targetInterval   =  0;
  silence          =  1;
  probabilityCount =  0;

  for(int i=0;i<NOISECYCLE;i++)
	energySample[i] = 0;

}

void Signal::process()
{
  
  static int counterAtBeat = -1;

  if(ctrl)
  {
    bool delayedBeat  = false;

	ctrl->currentBeat = false;


	if(!ctrl->adjust)
	if(beat)
	{
     counterAtBeat     = count;
	 ctrl->currentBeat = true;
	}

	if(ctrl->adjust)
	if(ctrl->period)
	{
     counterAtBeat     = count;  
	 ctrl->currentBeat = true;
	}

    if((counterAtBeat!=-1) && (count == (counterAtBeat + ctrl->delay) % NSAMPLES))	
    {
     if(ctrl->adjust)
	 if(ctrl->signal)
	   Beep(800,10);

	 counterAtBeat =-1;
     delayedBeat   = true; 
    }

    calcSilence();

    ctrl->update(count,delayedBeat,topInterval,probability,silence);

	if(((ctrl->targetInterval >= MININTERVAL) && (ctrl->targetInterval < MAXINTERVAL)) || ctrl->targetInterval == 0) 
	  targetInterval = ctrl->targetInterval;

	if(ctrl->resetSignal)
	{
	  reset();
	  ctrl->resetSignal = false;
	}
  }
  
  inc();
  
}

void Signal::calcSilence()
{
  static int energyCounter = 0;

  double sum = 0;


  for(int i=0;i<NSAMPLES;i++)
   sum += freq[i];

  energySample[energyCounter] = sum;

  ++energyCounter %= NOISECYCLE;


  sum = 0;

  for(int i=0;i<NOISECYCLE;i++)
   sum += energySample[i];

  silence = (sum / (NOISE * NOISECYCLE));

  if(silence < 0) 
	silence = 0;
  
} 

void Signal::inc()
{
  ++count %= NSAMPLES;

  countInc = (count + 1) % NSAMPLES;

  for(int i=0;i<NBANDS;i++)
  {
	band[i].count    = count;
    band[i].countInc = countInc;
  }
}
